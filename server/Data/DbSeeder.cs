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

        var username = configuration["DEMO_USERNAME"] ?? "demo";
        var password = configuration["DEMO_PASSWORD"] ?? "password123";

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = PasswordHasher.Hash(password)
        });

        await db.SaveChangesAsync();
    }
}
