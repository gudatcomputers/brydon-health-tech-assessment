using System.Security.Cryptography;
using BrydonServer.Auth;
using Microsoft.EntityFrameworkCore;

namespace BrydonServer.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        // SEED_RANDOM_USER_COUNT is for simulation/demo environments that need
        // a batch of throwaway users (e.g. to exercise the patient-portal
        // reporting sync) rather than the single named demo account.
        if (int.TryParse(configuration["SEED_RANDOM_USER_COUNT"], out var randomUserCount) && randomUserCount > 0)
        {
            for (var i = 0; i < randomUserCount; i++)
            {
                var username = $"patient-{RandomNumberGenerator.GetHexString(8).ToLowerInvariant()}";

                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    PasswordHash = PasswordHasher.Hash("password123")
                });
            }

            await db.SaveChangesAsync();
            return;
        }

        var demoUsername = configuration["DEMO_USERNAME"] ?? "demo";
        var demoPassword = configuration["DEMO_PASSWORD"] ?? "password123";

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = demoUsername,
            PasswordHash = PasswordHasher.Hash(demoPassword)
        });

        await db.SaveChangesAsync();
    }
}
