using API.Data;
using API.Entities.Oltp;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace API.Services
{
    public static class DbInitializer
    {
        /// <summary>
        /// Initialise la base de données avec un SuperAdmin par défaut si elle est vide
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OltpDbContext>();

            // Appliquer les migrations automatiquement
            await context.Database.MigrateAsync();

            // Vérifier si la base contient déjà des utilisateurs
            if (await context.Users.AnyAsync())
                return;

            // Créer le SuperAdmin par défaut
            CreatePasswordHash("SuperAdmin@123", out string passwordHash, out string passwordSalt);

            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@ecommerce.com",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = "SuperAdmin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}
