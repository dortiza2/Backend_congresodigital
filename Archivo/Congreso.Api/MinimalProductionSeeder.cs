using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;

namespace Congreso.Api;

public static class MinimalProductionSeeder
{
    public static async Task SeedAsync(CongresoDbContext db, IPasswordHasher hasher)
    {
        Console.WriteLine("[SeedMinimal] Iniciando seeding mínimo (producción)...");

        // 1) Asegurar roles clave por code (no duplica si existen)
        var requiredRoles = new (int Id, string Code, string Label, int Level)[]
        {
            (1, "staff", "Staff (Nivel 1)", 1),
            (2, "admin", "Administrador (Nivel 2)", 2),
            (3, "superadmin", "Super Administrador (Nivel 3)", 3),
            (4, "participant", "Participante", 0),
        };

        foreach (var rr in requiredRoles)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == rr.Code);
            if (role == null)
            {
                await db.Roles.AddAsync(new Role { Id = rr.Id, Code = rr.Code, Label = rr.Label, Level = rr.Level });
                Console.WriteLine($"[SeedMinimal] Rol creado: {rr.Code}");
            }
        }
        await db.SaveChangesAsync();

        // 2) Asegurar usuarios admin y superadmin por email si no existen
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@congreso.com");
        if (admin == null)
        {
            admin = new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440014"),
                Email = "admin@congreso.com",
                PasswordHash = hasher.HashPassword("Admin.123"),
                FullName = "Administrador Principal",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await db.Users.AddAsync(admin);
            Console.WriteLine("[SeedMinimal] Usuario admin creado");
        }

        var super = await db.Users.FirstOrDefaultAsync(u => u.Email == "superadmin@congreso.com");
        if (super == null)
        {
            super = new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440015"),
                Email = "superadmin@congreso.com",
                PasswordHash = hasher.HashPassword("SuperAdmin.123"),
                FullName = "Super Administrador",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await db.Users.AddAsync(super);
            Console.WriteLine("[SeedMinimal] Usuario superadmin creado");
        }
        await db.SaveChangesAsync();

        // 3) Asignar roles si faltan
        var adminRole = await db.Roles.FirstAsync(r => r.Code == "admin");
        var superRole = await db.Roles.FirstAsync(r => r.Code == "superadmin");

        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id))
        {
            await db.UserRoles.AddAsync(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            Console.WriteLine("[SeedMinimal] Rol admin asignado a admin@congreso.com");
        }

        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == super.Id && ur.RoleId == superRole.Id))
        {
            await db.UserRoles.AddAsync(new UserRole { UserId = super.Id, RoleId = superRole.Id });
            Console.WriteLine("[SeedMinimal] Rol superadmin asignado a superadmin@congreso.com");
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[SeedMinimal] Seeding mínimo completado.");
    }
}