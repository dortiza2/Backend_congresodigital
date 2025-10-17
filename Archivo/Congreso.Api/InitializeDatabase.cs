using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;

public class DatabaseInitializer
{
    public static async Task InitializeAsync(CongresoDbContext context, IPasswordHasher passwordHasher)
    {
        // Crear la base de datos si no existe
        await context.Database.EnsureCreatedAsync();

        // Verificar si ya hay usuarios
        if (await context.Users.AnyAsync())
        {
            Console.WriteLine("La base de datos ya contiene usuarios.");
            return;
        }

        Console.WriteLine("Inicializando base de datos con usuarios de prueba...");

        // Crear usuarios de prueba
        var users = new List<User>
        {
            new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                Email = "admindev@test.com",
                PasswordHash = "$2a$12$looRl7uhSIRY/dQSrenKJegXm/QWvVFncMQwTHjCX1ZJAM5PsDcfq", // AdminDev123!
                FullName = "Admin Dev",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active"
            },
            new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                Email = "admin@test.com",
                PasswordHash = "$2a$12$jDa/qRwuDEQiYRdvfStYUeOLuwmStofIDksJS6puws09FEPdbM9uW", // Admin123!
                FullName = "Admin",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active"
            },
            new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                Email = "asistente@test.com",
                PasswordHash = "$2a$12$nGgTMjOwf8CE9Y36TdaOhOmIMnf3qyfihooF4YcBfq7OoPMHjague", // Asistente123!
                FullName = "Asistente",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active"
            },
            new User
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
                Email = "estudiante@test.com",
                PasswordHash = "$2a$12$OZJAcVl.kCLJMzP7EefJhehdco4BB8386bHR.zg5FUJo6aYln.yvW", // Estudiante123!
                FullName = "Estudiante",
                OrgName = "UMG",
                IsUmg = true,
                Status = "active"
            }
        };

        // Crear roles
        var roles = new List<Role>
        {
            new Role { Id = 1, Code = "adminDev", Label = "Super Administrador" },
            new Role { Id = 2, Code = "admin", Label = "Administrador" },
            new Role { Id = 3, Code = "asistente", Label = "Asistente" }
        };

        // Agregar usuarios y roles
        await context.Users.AddRangeAsync(users);
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();

        // Crear relaciones usuario-rol
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = users[0].Id, RoleId = 1 }, // admindev@test.com -> adminDev
            new UserRole { UserId = users[1].Id, RoleId = 2 }, // admin@test.com -> admin
            new UserRole { UserId = users[2].Id, RoleId = 3 }  // asistente@test.com -> asistente
            // estudiante@test.com no tiene rol específico
        };

        await context.UserRoles.AddRangeAsync(userRoles);
        await context.SaveChangesAsync();

        Console.WriteLine("Base de datos inicializada correctamente.");
        Console.WriteLine("Usuarios creados:");
        Console.WriteLine("- admindev@test.com (AdminDev123!) - Super Admin");
        Console.WriteLine("- admin@test.com (Admin123!) - Admin");
        Console.WriteLine("- asistente@test.com (Asistente123!) - Asistente");
        Console.WriteLine("- estudiante@test.com (Estudiante123!) - Estudiante");
    }
}