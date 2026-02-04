using BCrypt.Net;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Data
{
    public static class DbSeeder
    {
        private const string AdminEmail = "admin@mentor-edu.local";
        private const string StudentEmail = "student@mentor-edu.local";
        private const string MentorEmail = "mentor@mentor-edu.local";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.AppUsers.AnyAsync(u => u.Email == AdminEmail || u.Email == StudentEmail || u.Email == MentorEmail))
            {
                return;
            }

            var now = DateTime.UtcNow;

            var admin = new User
            {
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            };

            var student = new User
            {
                Email = StudentEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                Role = UserRole.Student,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-2)
            };

            var mentor = new User
            {
                Email = MentorEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Mentor123!"),
                Role = UserRole.Mentor,
                IsVerified = true,
                IsActive = true,
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-2)
            };

            await context.AppUsers.AddRangeAsync(admin, student, mentor);
            await context.SaveChangesAsync();

            // Seed wallet for student (1,000,000 VND)
            var studentUser = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == StudentEmail);
            if (studentUser != null)
            {
                var wallet = new Wallet
                {
                    UserId = studentUser.Id,
                    Balance = 1_000_000,
                    LockedBalance = 0,
                    Currency = "VND",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await context.Wallets.AddAsync(wallet);
                await context.SaveChangesAsync();
            }
        }
    }
}