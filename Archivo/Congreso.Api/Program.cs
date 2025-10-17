using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Middleware;
using Congreso.Api.Models;
using Congreso.Api.Models.Configuration;
using Congreso.Api.Services;
using Congreso.Api.Filters;
using Congreso.Api.Utils;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Npgsql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Congreso.Api.Infrastructure;
using Congreso.Api.Infrastructure.HealthChecks;
using Congreso.Api.Repositories;
using Congreso.Api.Configuration;
using Congreso.Api.Middleware;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables desde .env si existe (solo backend)
try
{
    var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var t = line.Trim();
            if (string.IsNullOrWhiteSpace(t) || t.StartsWith("#")) continue;
            var idx = t.IndexOf('=');
            if (idx <= 0) continue;
            var key = t.Substring(0, idx).Trim();
            var val = t.Substring(idx + 1).Trim().Trim('\'', '"');
            Environment.SetEnvironmentVariable(key, val);
        }
    }
}
catch { /* ignore dotenv load errors */ }

// Resolver cadena de conexión, priorizando DATABASE_URL (Neon)
string cs;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string ResolvePlaceholders(string s)
{
    return Regex.Replace(s, @"\$\{([A-Za-z_][A-Za-z0-9_]*)(?::-(.*?))?\}", m =>
    {
        var key = m.Groups[1].Value;
        var defVal = m.Groups[2].Success ? m.Groups[2].Value : string.Empty;
        var env = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(env) ? defVal : env;
    });
}

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        // Ajustes para Neon: SSL requerido, pooling moderado y multiplexing (sin Keepalive)
        cs = $"Host={host};Port={port};Database={db};Username={user};Password={pass};Ssl Mode=Require;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=20;Timeout=30;Command Timeout=60";
    }
    catch
    {
        // Fallback a configuración previa si el URL no se puede parsear
        var def = builder.Configuration.GetConnectionString("Default");
        var pg = builder.Configuration.GetConnectionString("Postgres");
        cs = builder.Environment.IsDevelopment() ? def ?? pg ?? string.Empty : pg ?? def ?? string.Empty;
    }
}
else
{
    // Registrar NpgsqlDataSource usando connection strings de configuración
    var defaultCs = builder.Configuration.GetConnectionString("Default");
    var postgresCs = builder.Configuration.GetConnectionString("Postgres");

    cs = builder.Environment.IsDevelopment()
        ? defaultCs ?? postgresCs ?? string.Empty
        : postgresCs ?? defaultCs ?? string.Empty;

    // Si la cadena contiene placeholders tipo ${...}, resolver desde variables de entorno
    if (!string.IsNullOrWhiteSpace(cs) && cs.Contains("${"))
    {
        cs = ResolvePlaceholders(cs);
        if (string.IsNullOrWhiteSpace(cs) || cs.Contains("${"))
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "127.0.0.1";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var name = Environment.GetEnvironmentVariable("DB_NAME") ?? "congreso";
            var user = Environment.GetEnvironmentVariable("DB_USER") ?? "user_congreso";
            var pwd  = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
            cs = $"Host={host};Port={port};Database={name};Username={user};Password={pwd};Pooling=true;Maximum Pool Size=20;Timeout=30;Command Timeout=60;Trust Server Certificate=true;Ssl Mode=Require";
        }
    }
}

builder.Services.AddSingleton(sp =>
{
    try
    {
        var dsBuilder = new NpgsqlDataSourceBuilder(cs);
        return dsBuilder.Build();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] Error building NpgsqlDataSource: {ex.Message}");
        throw;
    }
});

// Add services to the container.
// Usar PostgreSQL para desarrollo
builder.Services.AddDbContext<CongresoDbContext>(options =>
{
    options.UseNpgsql(cs, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
    });
    if (builder.Environment.IsDevelopment())
    {
        if (bool.TryParse(builder.Configuration["ENABLE_SENSITIVE_LOGS"], out var sdl) && sdl)
        {
            options.EnableSensitiveDataLogging();
        }
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

builder.Services.AddControllers(options =>
    {
        // Register the global exception filter
        options.Filters.Add<DomainExceptionFilter>();
        // Register the global input validation filter
        options.Filters.Add<GlobalInputValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Ensure incoming JSON binds regardless of property name casing
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // Add custom converters for consistent serialization
        options.JsonSerializerOptions.Converters.Add(new GuidJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableGuidJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
    });

// Configure Email Settings
builder.Services.Configure<EmailSettings>(options =>
{
    options.Host = builder.Configuration["SMTP_HOST"] ?? "";
    options.Port = int.TryParse(builder.Configuration["SMTP_PORT"], out var port) ? port : 587;
    options.Username = builder.Configuration["SMTP_USERNAME"] ?? "";
    options.Password = builder.Configuration["SMTP_PASSWORD"] ?? "";
    options.FromEmail = builder.Configuration["SMTP_FROM_EMAIL"] ?? "";
    options.FromName = builder.Configuration["SMTP_FROM_NAME"] ?? "Congreso Digital UMG";
    options.EnableSsl = bool.TryParse(builder.Configuration["SMTP_ENABLE_SSL"], out var ssl) ? ssl : true;
    options.TimeoutSeconds = int.TryParse(builder.Configuration["SMTP_TIMEOUT_SECONDS"], out var timeout) ? timeout : 30;
    options.MaxRetries = int.TryParse(builder.Configuration["SMTP_MAX_RETRIES"], out var retries) ? retries : 3;
});

// Add Services
builder.Services.AddScoped<Congreso.Api.Services.EnrollmentService>();
builder.Services.AddScoped<Congreso.Api.Services.IAuthTokenService, Congreso.Api.Services.AuthTokenService>();
builder.Services.AddScoped<Congreso.Api.Services.IProfileService, Congreso.Api.Services.ProfileService>();
builder.Services.AddScoped<Congreso.Api.Services.IQrAttendanceService, Congreso.Api.Services.QrAttendanceService>();
builder.Services.AddScoped<Congreso.Api.Services.IPasswordHasher, Congreso.Api.Services.PasswordHasher>();
builder.Services.AddScoped<Congreso.Api.Services.ICheckInTokenService, Congreso.Api.Services.CheckInTokenService>();
builder.Services.AddScoped<Congreso.Api.Services.ISecureQrService, Congreso.Api.Services.SecureQrService>();
builder.Services.AddScoped<Congreso.Api.Services.IEmailService, Congreso.Api.Services.EmailService>();
builder.Services.AddSingleton<Congreso.Api.Services.IEmailLogService, Congreso.Api.Services.EmailLogService>();
// Old QR metrics collector - replaced by Infrastructure.MetricsCollector
builder.Services.AddSingleton<Congreso.Api.Services.IMetricsCollector, Congreso.Api.Services.MetricsCollector>();

// Add new services for user management and admin functionality
builder.Services.AddScoped<Congreso.Api.Services.IUserService, Congreso.Api.Services.UserService>();
builder.Services.AddScoped<Congreso.Api.Services.IStaffService, Congreso.Api.Services.StaffService>();
builder.Services.AddScoped<Congreso.Api.Services.IActivityAdminService, Congreso.Api.Services.ActivityAdminService>();
// Metrics de requests en memoria (solo dev)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<Congreso.Api.Services.IRequestMetrics, Congreso.Api.Services.InMemoryRequestMetrics>();
}

// Add Podium Services
builder.Services.AddScoped<IPodiumService, PodiumService>();
builder.Services.AddScoped<IPodiumRepository, PodiumRepository>();
builder.Services.AddScoped<IPodiumAuditService, PodiumAuditService>();

// Registrar servicios de infraestructura
builder.Services.AddSingleton<Congreso.Api.Infrastructure.MetricsCollector>();
builder.Services.AddSingleton<Congreso.Api.Infrastructure.ICertificateTemplateProvider, Congreso.Api.Infrastructure.CertificateTemplateProvider>();

// NUEVO: Registrar el nuevo MetricsCollector para Prometheus
builder.Services.AddSingleton<Congreso.Api.Infrastructure.IMetricsCollector, Congreso.Api.Infrastructure.MetricsCollector>();

builder.Services.AddScoped<CacheHealthCheck>();
builder.Services.AddScoped<CertificateEngineHealthCheck>();
builder.Services.AddScoped<ICertificatesService, CertificatesService>();
builder.Services.AddScoped<IEligibilityValidator, EligibilityValidator>();
builder.Services.AddScoped<ICertificateGenerator, CertificateGenerator>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IAuditService, CertificateAuditService>();
builder.Services.AddSingleton<CertificateExceptionHandler>();
builder.Services.AddScoped<CertificateAuthorization>();
builder.Services.AddHttpContextAccessor();

// Add security services
builder.Services.AddGlobalInputValidation();
builder.Services.AddMemoryCache(); // Required for rate limiting
builder.Services.AddRateLimiting(); // Register rate limiting services
builder.Services.AddAntiforgery(); // Required for CSRF protection

// Add FluentValidation
builder.Services.AddFluentValidation();

// Configure Cookie Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "auth-session";
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        
        // Configuración para DEV (HTTP) vs PROD (HTTPS)
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        }
        else
        {
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        
        // Sin redirecciones HTML en 401
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Política para staff (cualquier rol de staff)
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(ClaimTypes.Role, "Staff") ||
            context.User.HasClaim(ClaimTypes.Role, "Admin") ||
            context.User.HasClaim(ClaimTypes.Role, "SuperAdmin")
        ));

    // Política para administradores
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(ClaimTypes.Role, "Admin") ||
            context.User.HasClaim(ClaimTypes.Role, "SuperAdmin")
        ));

    // Política para super administradores
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "SuperAdmin"));

    // Política para estudiantes
    options.AddPolicy("StudentOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "Student"));

    // Política para certificados (estudiantes autenticados)
    options.AddPolicy("CertificatePolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.IsAuthenticated == true &&
            (context.User.HasClaim(ClaimTypes.Role, "Student") ||
             context.User.HasClaim(ClaimTypes.Role, "Staff") ||
             context.User.HasClaim(ClaimTypes.Role, "Admin") ||
             context.User.HasClaim(ClaimTypes.Role, "SuperAdmin"))
        ));

    // Política para requerir rol de admin o superior
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(ClaimTypes.Role, "Admin") ||
            context.User.HasClaim(ClaimTypes.Role, "SuperAdmin")
        ));

    // Política para requerir rol de super admin
    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireClaim(ClaimTypes.Role, "SuperAdmin"));

    // Política para requerir rol de staff o superior
    options.AddPolicy("RequireStaffOrHigher", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(ClaimTypes.Role, "Staff") ||
            context.User.HasClaim(ClaimTypes.Role, "Admin") ||
            context.User.HasClaim(ClaimTypes.Role, "SuperAdmin")
        ));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("https://congreso.umg.edu.gt", "https://congresodigital.org")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition", "X-Rate-Limit-Remaining", "X-Rate-Limit-Reset");
    });

    options.AddPolicy("FrontLocal", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3010", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition", "X-Rate-Limit-Remaining", "X-Rate-Limit-Reset");
    });
});

// Configure Data Protection for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"))
        .SetApplicationName("CongresoApi");
}

// Configure HSTS for production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Congreso Digital API",
        Version = "v1",
        Description = "API para la gestión de congresos, certificados y participantes",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Equipo Congreso Digital",
            Email = "soporte@congreso.local"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            System.Array.Empty<string>()
        }
    });

    options.OrderActionsBy(api => api.RelativePath);
    options.EnableAnnotations();
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });

    // Resolver conflictos de acciones con el mismo método/ruta (p.ej. duplicados en api/admin/activities)
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Evitar colisiones de schemaId entre tipos homónimos en distintos namespaces
    options.CustomSchemaIds(type =>
    {
        // Usar nombre calificado sin el assembly, reemplazando caracteres no válidos
        var fullName = type.FullName ?? type.Name;
        return fullName.Replace(".", "_").Replace("+", "_");
    });
});
// Swagger ya configurado arriba con opciones detalladas

// HealthChecks: validar Postgres leyendo v_health
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>(
        "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "postgres" }
    )
    .Add(new HealthCheckRegistration(
        "database",
        sp => new DatabaseHealthCheck(cs, sp.GetRequiredService<Congreso.Api.Infrastructure.MetricsCollector>()),
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "postgres", "podium", "certificates" }
    ))
    .AddCheck<CacheHealthCheck>(
        "cache",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cache", "memory", "podium" }
    )
    .AddCheck<CertificateEngineHealthCheck>(
        "certificate-engine",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "certificates", "templates", "pdf" }
    );

var app = builder.Build();

// Global exception handling is now handled by DomainExceptionFilter

// Configure the HTTP request pipeline.
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Congreso Digital API v1");
    options.DocumentTitle = "Congreso Digital API";
    options.DisplayRequestDuration();
    options.DefaultModelsExpandDepth(-1);
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.EnablePersistAuthorization();
});

if (!app.Environment.IsDevelopment())
{
    // Enable HSTS in production
    app.UseHsts();
    app.UseHttpsRedirection();
}

// NUEVO: Structured logging middleware (primero en el pipeline)
app.UseStructuredLogging();

// Security headers middleware (must be early in pipeline)
app.UseSecurityHeaders();

// NUEVO: Rate limiting middleware (antes de autenticación)
app.UseRateLimiting();

// NUEVO: JWT Validation middleware
app.UseJwtValidation();

// NUEVO: CSRF Protection middleware
app.UseCsrfProtection();

app.UseStaticFiles(); // Habilitar archivos estáticos para servir certificados

// CORS debe ir antes de Authentication y Authorization
if (app.Environment.IsDevelopment())
{
    app.UseCors("FrontLocal");
}
else
{
    app.UseCors("FrontendPolicy");
}

// Bloquear rutas no soportadas explícitamente con 410 justo antes del routing
app.UseMiddleware<Congreso.Api.Middlewares.NotImplementedPathsMiddleware>();
app.UseRouting();

app.UseAuthentication();
app.UseProfileClaims(); // Enriquecer claims después de autenticación
app.UseMiddleware<Congreso.Api.Middleware.RoleAuthorizationMiddleware>(); // Role-based authorization middleware
app.UseAuthorization();

// Certificate exception handling middleware
app.UseCertificateExceptionHandling();

// Logging estructurado y métricas ya están configurados arriba
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => 
{
    var version = System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "1.0.0";
    
    return Results.Ok(new { 
        ok = true, 
        version = version,
        time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

// Health check endpoints for observability
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("db") || r.Tags.Contains("postgres"),
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("cache") || r.Tags.Contains("certificates"),
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(result);
    }
});

// Endpoint de health de Postgres
app.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Name == "postgres"
});

// NUEVO: Métricas Prometheus endpoint
app.MapGet("/metrics", (Congreso.Api.Infrastructure.IMetricsCollector metricsCollector) =>
{
    var metrics = metricsCollector.GetPrometheusMetrics();
    return Results.Content(metrics, "text/plain");
});

// Endpoint interno de métricas (solo desarrollo)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/internal/metrics", (Congreso.Api.Services.IRequestMetrics metrics) =>
    {
        return Results.Ok(new { ok = true, ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), data = metrics.GetSnapshot() });
    });
}

// Baseline opcional de migraciones EF si se pasa --baseline-ef
if (args.Contains("--baseline-ef"))
{
    try
    {
        using var conn = new Npgsql.NpgsqlConnection(cs);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";
        var countObj = cmd.ExecuteScalar();
        var count = countObj is long l ? l : (countObj is int i ? i : 0);
        if (count == 0)
        {
            var rows = new (string Id, string Pv)[] {
                ("20250916002102_InitialMigration","9.0.8"),
                ("20250918082703_UpdateModelChanges","9.0.9"),
                ("20250919170130_RemoveStudentAccountColumns","9.0.8"),
                ("20250929000742_UpdateModelForTesting","9.0.8"),
                ("20251013221529_AutoFixSchema","9.0.8"),
                ("20251015191900_FixModelsAndSeeder","9.0.8"),
                ("20251015214115_SyncSchemaUsersUuid","9.0.8"),
                ("20251015232027_SyncPendingModel_20251015","9.0.8"),
            };
            foreach (var row in rows)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\",\"ProductVersion\") VALUES (@id,@pv) ON CONFLICT (\"MigrationId\") DO NOTHING;";
                ins.Parameters.Add(new Npgsql.NpgsqlParameter("id", row.Id));
                ins.Parameters.Add(new Npgsql.NpgsqlParameter("pv", row.Pv));
                ins.ExecuteNonQuery();
            }
            Console.WriteLine("[EF] Baseline applied to __EFMigrationsHistory.");
        }
        else
        {
            Console.WriteLine("[EF] __EFMigrationsHistory already has entries, skipping baseline.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EF] Baseline failed: {ex.Message}");
    }
    return;
}


// Seed de datos en desarrollo
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CongresoDbContext>();
        try
        {
            // Aplicar migraciones pendientes
            db.Database.Migrate();
            Console.WriteLine("[DB] Migraciones aplicadas correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Error aplicando migraciones: {ex.Message}");
        }

        var hasher = scope.ServiceProvider.GetRequiredService<Congreso.Api.Services.IPasswordHasher>();
        try
        {
            // Ejecutar seeder comprensivo
            ComprehensiveDatabaseSeeder.SeedTestDataAsync(db, hasher).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seeder] Error durante el seed: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Error en seed de desarrollo: {ex.Message}");
    }
}

app.Run();

// Implementación de HealthCheck para Postgres
class PostgresHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;
    public PostgresHealthCheck(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            // No depender de vistas ni permisos específicos: prueba mínima de conectividad
            await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result is null)
            {
                return HealthCheckResult.Unhealthy("Connectivity check returned no result.");
            }
            return HealthCheckResult.Healthy("Postgres health check passed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Postgres health check failed: {ex.Message}");
        }
    }
}
