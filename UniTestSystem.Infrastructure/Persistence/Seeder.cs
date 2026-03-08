using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using BCrypt.Net;

namespace UniTestSystem.Infrastructure.Persistence
{
    public static class Seeder
    {
        public static async Task RunAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // 1. Migrate
            await db.Database.MigrateAsync();

            // 2. Default Admin User
            if (!await db.Users.AnyAsync(u => u.Role == Role.Admin))
            {
                var admin = new User
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "System Admin",
                    Email = "admin@unitest.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = Role.Admin,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(admin);
                
                // Add to Students/Lecturers tables if polymorphic? 
                // Based on previous audits, User is base. 
                // Let's just add to Users for now.
                
                await db.SaveChangesAsync();
            }

        // 3. Permissions are handled by IPermissionService.EnsureDefaultAsync() 
        // which is called in Program.cs right after Seeder.RunAsync
        }

        public static async Task ResetAllJsonFilesAsync(IServiceProvider sp, bool reseed = true)
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            if (reseed) await RunAsync(sp);
        }
    }
}
