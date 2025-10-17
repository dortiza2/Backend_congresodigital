using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;
using System.Security.Claims;
using System.Text;

public class ComprehensiveDatabaseSeeder
{
    public static async Task SeedTestDataAsync(CongresoDbContext context, IPasswordHasher passwordHasher)
    {
        Console.WriteLine("=== INICIANDO SEED COMPLETO DE BASE DE DATOS ===");
        
        // 1. CREAR ROLES SEGÚN TAREASPM
        Console.WriteLine("1. Creando roles del sistema...");
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Id = 1, Code = "staff", Label = "Staff (Nivel 1)", Level = 1 },
                new Role { Id = 2, Code = "admin", Label = "Administrador (Nivel 2)", Level = 2 },
                new Role { Id = 3, Code = "superadmin", Label = "Super Administrador (Nivel 3)", Level = 3 },
                new Role { Id = 4, Code = "participant", Label = "Participante", Level = 0 }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Roles creados exitosamente");
        }

        // 2. CREAR USUARIOS DE PRUEBA SEGÚN TAREASPM
        Console.WriteLine("2. Creando usuarios de prueba...");
        if (!await context.Users.AnyAsync(u => u.Email.StartsWith("david")))
        {
            var testUsers = new List<User>
            {
                // STAFF LEVEL 1 - David1, David2, David3
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
                    Email = "david1@congreso.com",
                    PasswordHash = passwordHasher.HashPassword("D@vid.123"),
                    FullName = "David Ortiz - Staff 1",
                    OrgName = "UMG",
                    IsUmg = true,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440011"),
                    Email = "david2@congreso.com",
                    PasswordHash = passwordHasher.HashPassword("D@vid.123"),
                    FullName = "David Ortiz - Staff 2",
                    OrgName = "UMG",
                    IsUmg = true,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440012"),
                    Email = "david3@congreso.com",
                    PasswordHash = passwordHasher.HashPassword("D@vid.123"),
                    FullName = "David Ortiz - Staff 3",
                    OrgName = "UMG",
                    IsUmg = true,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                // USUARIO EXTERNO - fiwax76533@lorkex.com
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440013"),
                    Email = "fiwax76533@lorkex.com",
                    PasswordHash = passwordHasher.HashPassword("Test.1234"),
                    FullName = "Usuario Externo de Prueba",
                    OrgName = "Externo",
                    IsUmg = false,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                // ADMIN LEVEL 2
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440014"),
                    Email = "admin@congreso.com",
                    PasswordHash = passwordHasher.HashPassword("Admin.123"),
                    FullName = "Administrador Principal",
                    OrgName = "UMG",
                    IsUmg = true,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                // SUPERADMIN LEVEL 3
                new User
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440015"),
                    Email = "superadmin@congreso.com",
                    PasswordHash = passwordHasher.HashPassword("SuperAdmin.123"),
                    FullName = "Super Administrador",
                    OrgName = "UMG",
                    IsUmg = true,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await context.Users.AddRangeAsync(testUsers);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Usuarios de prueba creados exitosamente");
        }

        // 3. ASIGNAR ROLES A USUARIOS
        Console.WriteLine("3. Asignando roles a usuarios...");
        if (!await context.UserRoles.AnyAsync())
        {
            var userRoles = new List<UserRole>
            {
                // Staff Level 1
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440010"), RoleId = 1 }, // david1@congreso.com
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440011"), RoleId = 1 }, // david2@congreso.com
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440012"), RoleId = 1 }, // david3@congreso.com
                // Usuario externo
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440013"), RoleId = 4 }, // fiwax76533@lorkex.com
                // Admin Level 2
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440014"), RoleId = 2 }, // admin@congreso.com
                // SuperAdmin Level 3
                new UserRole { UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440015"), RoleId = 3 }  // superadmin@congreso.com
            };
            
            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Roles asignados exitosamente");
        }

        // 4. CREAR SPEAKERS/PONENTES
        Console.WriteLine("4. Creando speakers/presentadores...");
        if (!await context.Speakers.AnyAsync())
        {
            var speakers = new List<Speaker>
            {
                new Speaker
                {
                    Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440001"),
                    FullName = "Dr. Carlos González",
                    Bio = "Experto en Inteligencia Artificial y Machine Learning con 15 años de experiencia",
                    PhotoUrl = "https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Professional%20male%20speaker%20portrait%2C%20confident%20smile%2C%20business%20attire%2C%20university%20professor%20style&image_size=square",
                    ContactEmail = "cgonzalez@umg.edu.gt",
                    OrgName = "Universidad Mariano Gálvez",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
               },
                new Speaker
                {
                    Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440002"),
                    FullName = "Dra. María Rodríguez",
                    Bio = "Especialista en Ciberseguridad y Protección de Datos, consultora internacional",
                    PhotoUrl = "https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Professional%20female%20speaker%20portrait%2C%20confident%20expression%2C%20modern%20business%20attire%2C%20tech%20conference%20speaker&image_size=square",
                    ContactEmail = "mrodriguez@cybersec.com",
                    OrgName = "CyberSecurity Solutions",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
               },
                new Speaker
                {
                    Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440003"),
                    FullName = "Ing. José Martínez",
                    Bio = "Desarrollador de Software Senior, experto en arquitecturas cloud y microservicios",
                    PhotoUrl = "https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Professional%20male%20developer%20portrait%2C%20casual%20tech%20attire%2C%20friendly%20smile%2C%20software%20conference%20speaker&image_size=square",
                    ContactEmail = "jmartinez@techcorp.com",
                    OrgName = "TechCorp Guatemala",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
               },
                new Speaker
                {
                    Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440004"),
                    FullName = "Dra. Ana López",
                    Bio = "Investigadora en Tecnologías Emergentes y Transformación Digital",
                    PhotoUrl = "https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Professional%20female%20researcher%20portrait%2C%20academic%20attire%2C%20innovative%20presentation%20style%2C%20university%20researcher&image_size=square",
                    ContactEmail = "alopez@research.umg.edu.gt",
                    OrgName = "UMG Research Lab",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            
            await context.Speakers.AddRangeAsync(speakers);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Speakers creados exitosamente");
        }

        // 5. CREAR ACTIVIDADES (CHARLAS, TALLERES, COMPETENCIAS)
        Console.WriteLine("5. Creando actividades (charlas, talleres, competencias)...");
        if (!await context.Activities.AnyAsync())
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(7); // Próxima semana
            var activities = new List<Activity>
            {
                // CHARLAS
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440001"),
                    Title = "Inteligencia Artificial en la Industria Actual",
                    Description = "Exploración de casos de uso de IA en diferentes sectores industriales y su impacto en la productividad",
                    ActivityType = ActivityType.CHARLA,
                    StartTime = baseDate.AddHours(9), // 9:00 AM
                    EndTime = baseDate.AddHours(10), // 10:00 AM
                    Location = "Auditorio Principal UMG",
                    Capacity = 200,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440002"),
                    Title = "Ciberseguridad en la Era Digital",
                    Description = "Estrategias modernas para proteger activos digitales en un mundo cada vez más conectado",
                    ActivityType = ActivityType.CHARLA,
                    StartTime = baseDate.AddHours(14), // 2:00 PM
                    EndTime = baseDate.AddHours(15), // 3:00 PM
                    Location = "Auditorio Principal UMG",
                    Capacity = 150,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // TALLERES
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440003"),
                    Title = "Taller: Desarrollo de APIs RESTful",
                    Description = "Aprende a diseñar y construir APIs RESTful robustas y escalables usando .NET Core",
                    ActivityType = ActivityType.TALLER,
                    StartTime = baseDate.AddDays(1).AddHours(10), // Día siguiente 10:00 AM
                    EndTime = baseDate.AddDays(1).AddHours(12), // 12:00 PM
                    Location = "Laboratorio de Computación 1",
                    Capacity = 30,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440004"),
                    Title = "Taller: Introducción a Machine Learning",
                    Description = "Primeros pasos en el mundo del machine learning con Python y scikit-learn",
                    ActivityType = ActivityType.TALLER,
                    StartTime = baseDate.AddDays(1).AddHours(15), // Día siguiente 3:00 PM
                    EndTime = baseDate.AddDays(1).AddHours(17), // 5:00 PM
                    Location = "Laboratorio de Computación 2",
                    Capacity = 25,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // COMPETENCIAS
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440005"),
                    Title = "Competencia: Hackathon de Innovación",
                    Description = "Competencia de desarrollo de software con premios para los mejores proyectos innovadores",
                    ActivityType = ActivityType.COMPETENCIA,
                    StartTime = baseDate.AddDays(2).AddHours(8), // Tercer día 8:00 AM
                    EndTime = baseDate.AddDays(2).AddHours(18), // 6:00 PM
                    Location = "Centro de Innovación UMG",
                    Capacity = 50,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Activity
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440006"),
                    Title = "Competencia: CTF de Ciberseguridad",
                    Description = "Capture The Flag con desafíos de ciberseguridad para todos los niveles",
                    ActivityType = ActivityType.COMPETENCIA,
                    StartTime = baseDate.AddDays(2).AddHours(13), // Tercer día 1:00 PM
                    EndTime = baseDate.AddDays(2).AddHours(17), // 5:00 PM
                    Location = "Laboratorio de Seguridad",
                    Capacity = 40,
                    Published = true,
                    RequiresEnrollment = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            
            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Actividades creadas exitosamente");
        }

        // 6. SPEAKERS YA ESTÁN RELACIONADOS CON ACTIVIDADES EN EL SEEDER
        Console.WriteLine("6. Speakers ya están relacionados con actividades...");

        // 7. CREAR GANADORES PARA 2025
        Console.WriteLine("7. Creando ganadores para 2025...");
        if (!await context.Winners.AnyAsync(w => w.EditionYear == 2025))
        {
            var candidates = new (Guid activityId, Guid userId, short place)[]
            {
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440001"), Guid.Parse("550e8400-e29b-41d4-a716-446655440011"), (short)1),
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440002"), Guid.Parse("550e8400-e29b-41d4-a716-446655440012"), (short)2),
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440003"), Guid.Parse("550e8400-e29b-41d4-a716-446655440013"), (short)3)
            };

            var winners = new List<Winner>();
            foreach (var c in candidates)
            {
                var activityExists = await context.Activities.AnyAsync(a => a.Id == c.activityId);
                var userExists = await context.Users.AnyAsync(u => u.Id == c.userId);
                if (activityExists && userExists)
                {
                    winners.Add(new Winner
                    {
                        EditionYear = 2025,
                        ActivityId = c.activityId,
                        Place = c.place,
                        UserId = c.userId
                    });
                }
            }

            if (winners.Count > 0)
            {
                await context.Winners.AddRangeAsync(winners);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Ganadores 2025 creados exitosamente (insertados: {winners.Count})");
            }
            else
            {
                Console.WriteLine("↷ Ganadores 2025 omitidos: faltan actividades/usuarios esperados");
            }
        }

        // 7b. CREAR GANADORES PARA 2023
        Console.WriteLine("7b. Creando ganadores para 2023...");
        if (!await context.Winners.AnyAsync(w => w.EditionYear == 2023))
        {
            var candidates2023 = new (Guid activityId, Guid userId, short place)[]
            {
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440001"), Guid.Parse("550e8400-e29b-41d4-a716-446655440010"), (short)1),
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440004"), Guid.Parse("550e8400-e29b-41d4-a716-446655440011"), (short)2),
                (Guid.Parse("770e8400-e29b-41d4-a716-446655440006"), Guid.Parse("550e8400-e29b-41d4-a716-446655440012"), (short)3)
            };

            var winners2023 = new List<Winner>();
            foreach (var c in candidates2023)
            {
                var activityExists = await context.Activities.AnyAsync(a => a.Id == c.activityId);
                var userExists = await context.Users.AnyAsync(u => u.Id == c.userId);
                if (activityExists && userExists)
                {
                    winners2023.Add(new Winner
                    {
                        EditionYear = 2023,
                        ActivityId = c.activityId,
                        Place = c.place,
                        UserId = c.userId
                    });
                }
            }

            if (winners2023.Count > 0)
            {
                await context.Winners.AddRangeAsync(winners2023);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Ganadores 2023 creados exitosamente (insertados: {winners2023.Count})");
            }
            else
            {
                Console.WriteLine("↷ Ganadores 2023 omitidos: faltan actividades/usuarios esperados");
            }
        }

        // 8. CREAR INSCRIPCIONES DE PRUEBA
        Console.WriteLine("8. Creando inscripciones de prueba...");
        if (!await context.Enrollments.AnyAsync())
        {
            var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440013"); // fiwax76533@lorkex.com
            var enrollments = new List<Enrollment>
            {
                // Inscripción en charla de IA
                new Enrollment
                {
                    Id = Guid.Parse("aa0e8400-e29b-41d4-a716-446655440001"),
                    UserId = userId,
                    ActivityId = Guid.Parse("770e8400-e29b-41d4-a716-446655440001"),
                    QrCodeId = GenerateQRCode(),
                    Attended = false
                },
                // Inscripción en taller de APIs
                new Enrollment
                {
                    Id = Guid.Parse("aa0e8400-e29b-41d4-a716-446655440002"),
                    UserId = userId,
                    ActivityId = Guid.Parse("770e8400-e29b-41d4-a716-446655440003"),
                    QrCodeId = GenerateQRCode(),
                    Attended = false
                },
                // Inscripción en competencia de Hackathon
                new Enrollment
                {
                    Id = Guid.Parse("aa0e8400-e29b-41d4-a716-446655440003"),
                    UserId = userId,
                    ActivityId = Guid.Parse("770e8400-e29b-41d4-a716-446655440005"),
                    QrCodeId = GenerateQRCode(),
                    Attended = false
                }
            };
            
            await context.Enrollments.AddRangeAsync(enrollments);
            
            // Actualizar cupos disponibles
            foreach (var enrollment in enrollments)
            {
                var activity = await context.Activities.FindAsync(enrollment.ActivityId);
                if (activity != null && activity.Capacity > 0)
                {
                    activity.Capacity--;
                }
            }
            
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Inscripciones de prueba creadas exitosamente");
        }

        Console.WriteLine("=== SEED COMPLETO FINALIZADO EXITOSAMENTE ===");
        Console.WriteLine("");
        Console.WriteLine("CUENTAS DE PRUEBA CREADAS:");
        Console.WriteLine("STAFF (Nivel 1):");
        Console.WriteLine("  - david1@congreso.com / D@vid.123");
        Console.WriteLine("  - david2@congreso.com / D@vid.123");
        Console.WriteLine("  - david3@congreso.com / D@vid.123");
        Console.WriteLine("");
        Console.WriteLine("USUARIO EXTERNO:");
        Console.WriteLine("  - fiwax76533@lorkex.com / Test.1234");
        Console.WriteLine("");
        Console.WriteLine("ADMINISTRADORES:");
        Console.WriteLine("  - admin@congreso.com / Admin.123");
        Console.WriteLine("  - superadmin@congreso.com / SuperAdmin.123");
        Console.WriteLine("");
        Console.WriteLine("ACTIVIDADES CREADAS:");
        Console.WriteLine("  - 2 Charlas: IA, Ciberseguridad");
        Console.WriteLine("  - 2 Talleres: APIs RESTful, Machine Learning");
        Console.WriteLine("  - 2 Competencias: Hackathon, CTF");
        Console.WriteLine("");
        Console.WriteLine("PODIO 2025 CREADO CON GANADORES");
        Console.WriteLine("INSCRIPCIONES DE PRUEBA CONFIGURADAS");
    }

    private static string GenerateQRCode()
    {
        var random = new Random();
        var qrCode = new StringBuilder();
        qrCode.Append("QR");
        qrCode.Append(DateTime.UtcNow.ToString("yyyyMMdd"));
        qrCode.Append(random.Next(1000, 9999));
        return qrCode.ToString();
    }
}