using Microsoft.EntityFrameworkCore;
using SecureLoginSystemNew.Models;
using SecureLoginSystemNew.Services;
using BCrypt.Net;
using System;
using System.Linq;

namespace SecureLoginSystemNew.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, TwoFactorService twoFactorService, bool isTestEnvironment)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                var user = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("adminTest1"),
                    Email = "admin@admin.com",
                    IsEmailVerified = true,
                    TwoFactorEnabled = false,
                    TwoFactorSecret = twoFactorService.GenerateSecretKey(),
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(user);
                context.SaveChanges();
            }
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}