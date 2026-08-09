using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var student = new User { FullName = "Sample Student", Email = "student@school.com", PasswordHash = "", Role = UserRoles.Student };
            var admin = new User { FullName = "School Administrator", Email = "admin@school.com", PasswordHash = "", Role = UserRoles.Admin };
            student.PasswordHash = hasher.HashPassword(student, "Student123!");
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
            db.Users.AddRange(student, admin);
        }

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new PaperService { Name = "Enrollment Certificate", Description = "Official certificate proving student enrollment." },
                new PaperService { Name = "Grade Transcript", Description = "Official academic grade transcript." },
                new PaperService { Name = "Attendance Certificate", Description = "Official attendance certificate." });
        }

        await db.SaveChangesAsync();
    }
}
