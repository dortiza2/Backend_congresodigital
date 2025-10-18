using System;
using System.Threading.Tasks;
using Npgsql;
using BCrypt.Net;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            string BuildConnectionStringFromDatabaseUrl(string dbUrl)
            {
                var uri = new Uri(dbUrl);
                var userInfo = uri.UserInfo.Split(':', 2);
                var host = (uri.Host ?? string.Empty).Trim();
                var port = uri.Port > 0 ? uri.Port : 5432;
                var db = (uri.AbsolutePath ?? string.Empty).Trim('/').Trim();
                var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
                var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(user))
                    throw new ArgumentException("Invalid DATABASE_URL: missing host/db/user");
                return $"Host={host};Port={port};Database={db};Username={user};Password={pass};Ssl Mode=Require;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=20;Timeout=30;Command Timeout=60";
            }

            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            string? cs = null;
            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                cs = BuildConnectionStringFromDatabaseUrl(databaseUrl!);
            }
            else
            {
                // Intenta ConnectionStrings o variables sueltas (DB_*)
                var csDefault = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
                var csDefaultConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                var csPostgres = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
                cs = csDefault ?? csDefaultConn ?? csPostgres;
                if (string.IsNullOrWhiteSpace(cs))
                {
                    var host = Environment.GetEnvironmentVariable("DB_HOST");
                    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
                    var db = Environment.GetEnvironmentVariable("DB_NAME");
                    var user = Environment.GetEnvironmentVariable("DB_USER");
                    var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
                    if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(db) && !string.IsNullOrWhiteSpace(user))
                    {
                        cs = $"Host={host};Port={port};Database={db};Username={user};Password={pass};Ssl Mode=Require;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=20;Timeout=30;Command Timeout=60";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(cs))
            {
                Console.Error.WriteLine("No se encontró cadena de conexión. Define `DATABASE_URL` o `ConnectionStrings__Default` o variables `DB_*`.");
                return 1;
            }

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            // Helper local para exists y scalar
            async Task<bool> ExistsAsync(string sql, params (string, object)[] parameters)
            {
                await using var c = new NpgsqlCommand(sql, conn, tx);
                foreach (var (n, v) in parameters)
                    c.Parameters.AddWithValue(n, v);
                var res = await c.ExecuteScalarAsync();
                return res != null && res != DBNull.Value;
            }
            async Task<object?> ScalarAsync(string sql, params (string, object)[] parameters)
            {
                await using var c = new NpgsqlCommand(sql, conn, tx);
                foreach (var (n, v) in parameters)
                    c.Parameters.AddWithValue(n, v);
                return await c.ExecuteScalarAsync();
            }

            // Upsert de roles por CODE (no asume PK/índice único)
            async Task<int> UpsertRoleByCode(int desiredId, string code, string name, string label, int level)
            {
                var existingIdObj = await ScalarAsync("SELECT id FROM roles WHERE code = @code LIMIT 1", ("@code", code));
                if (existingIdObj != null && existingIdObj != DBNull.Value)
                {
                    var existingId = Convert.ToInt32(existingIdObj);
                    await using var upd = new NpgsqlCommand("UPDATE roles SET name=@name, label=@label, level=@level WHERE id=@id", conn, tx);
                    upd.Parameters.AddWithValue("@id", existingId);
                    upd.Parameters.AddWithValue("@name", name);
                    upd.Parameters.AddWithValue("@label", label);
                    upd.Parameters.AddWithValue("@level", level);
                    await upd.ExecuteNonQueryAsync();
                    return existingId;
                }
                else
                {
                    await using var ins = new NpgsqlCommand("INSERT INTO roles (id, code, name, label, level) VALUES (@id, @code, @name, @label, @level)", conn, tx);
                    ins.Parameters.AddWithValue("@id", desiredId);
                    ins.Parameters.AddWithValue("@code", code);
                    ins.Parameters.AddWithValue("@name", name);
                    ins.Parameters.AddWithValue("@label", label);
                    ins.Parameters.AddWithValue("@level", level);
                    await ins.ExecuteNonQueryAsync();
                    return desiredId;
                }
            }

            // Upsert usuarios por EMAIL; devuelve user_id (bigint)
            async Task<long> UpsertUserByEmail(string email, string plainPwd, string fullName, string roleCode, Guid idGuid)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPwd);
                var idObj = await ScalarAsync("SELECT id FROM users WHERE email=@em LIMIT 1", ("@em", email));
                if (idObj != null && idObj != DBNull.Value)
                {
                    var userId = Convert.ToInt64(idObj);
                    await using var upd = new NpgsqlCommand(
                        // No escribimos la columna enum 'role' para evitar diferencias de esquema; usamos user_roles abajo
                        "UPDATE users SET password_hash=@ph, full_name=@fn, org_name=@org, is_umg=@umg, status='active', id_guid=@idg, updated_at=now(), is_active=true WHERE id=@id",
                        conn, tx);
                    upd.Parameters.AddWithValue("@id", userId);
                    upd.Parameters.AddWithValue("@ph", passwordHash);
                    upd.Parameters.AddWithValue("@fn", fullName);
                    upd.Parameters.AddWithValue("@org", "UMG");
                    upd.Parameters.AddWithValue("@umg", true);
                    upd.Parameters.AddWithValue("@idg", idGuid);
                    await upd.ExecuteNonQueryAsync();
                    return userId;
                }
                else
                {
                    await using var ins = new NpgsqlCommand(
                        // No incluimos la columna enum 'role' para compatibilidad
                        "INSERT INTO users (email, password_hash, full_name, org_name, is_umg, status, created_at, updated_at, is_active, id_guid) " +
                        "VALUES (@em, @ph, @fn, @org, @umg, 'active', now(), now(), true, @idg) RETURNING id",
                        conn, tx);
                    ins.Parameters.AddWithValue("@em", email);
                    ins.Parameters.AddWithValue("@ph", passwordHash);
                    ins.Parameters.AddWithValue("@fn", fullName);
                    ins.Parameters.AddWithValue("@org", "UMG");
                    ins.Parameters.AddWithValue("@umg", true);
                    ins.Parameters.AddWithValue("@idg", idGuid);
                    var newId = await ins.ExecuteScalarAsync();
                    return Convert.ToInt64(newId);
                }
            }

            async Task EnsureUserRole(long userId, int roleId)
            {
                if (!await ExistsAsync("SELECT 1 FROM user_roles WHERE user_id=@u AND role_id=@r LIMIT 1", ("@u", userId), ("@r", roleId)))
                {
                    await using var ins = new NpgsqlCommand(
                        // Compatibilidad con esquemas que no tienen columnas de auditoría
                        "INSERT INTO user_roles (user_id, role_id) VALUES (@u, @r)",
                        conn, tx);
                    ins.Parameters.AddWithValue("@u", userId);
                    ins.Parameters.AddWithValue("@r", roleId);
                    await ins.ExecuteNonQueryAsync();
                }
            }

            // 1) Roles requeridos (por CODE)
            var staffId = await UpsertRoleByCode(1, "staff", "Staff", "Staff (Nivel 1)", 1);
            var adminIdRole = await UpsertRoleByCode(2, "admin", "Admin", "Administrador (Nivel 2)", 2);
            var superIdRole = await UpsertRoleByCode(3, "superadmin", "Super Admin", "Super Administrador (Nivel 3)", 3);
            var participantId = await UpsertRoleByCode(4, "participant", "Participant", "Participante", 0);

            // 2) Usuarios admin y superadmin (por EMAIL, con id_guid para trazabilidad)
            var adminGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440014");
            var superGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440015");
            var adminUserId = await UpsertUserByEmail("admin@congreso.com", "Admin.123", "Administrador Principal", "admin", adminGuid);
            var superUserId = await UpsertUserByEmail("superadmin@congreso.com", "SuperAdmin.123", "Super Administrador", "superadmin", superGuid);

            // 3) Asignar roles si faltan
            await EnsureUserRole(adminUserId, adminIdRole);
            await EnsureUserRole(superUserId, superIdRole);

            await tx.CommitAsync();
            Console.WriteLine("[SQL Seed] Seeding mínimo completado (roles, admin, superadmin).\n - admin@congreso.com / Admin.123\n - superadmin@congreso.com / SuperAdmin.123");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error en SQL Seed.Runner: {ex}");
            return 2;
        }
    }
}