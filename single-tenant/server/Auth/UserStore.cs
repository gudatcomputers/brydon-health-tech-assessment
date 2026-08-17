using BrydonServer.Data;
using Microsoft.EntityFrameworkCore;

namespace BrydonServer.Auth;

public class UserStore(AppDbContext db)
{
    public Task<User?> FindByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

    public async Task<User> CreateAsync(string username, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public Task<int?> GetTokenVersionAsync(Guid id) =>
        db.Users.Where(u => u.Id == id).Select(u => (int?)u.TokenVersion).FirstOrDefaultAsync();

    // Atomic UPDATE so concurrent logouts can't race and drop an increment.
    public Task IncrementTokenVersionAsync(Guid id) =>
        db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1));
}
