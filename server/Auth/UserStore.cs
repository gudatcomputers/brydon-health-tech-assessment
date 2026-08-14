using BrydonServer.Data;
using Microsoft.EntityFrameworkCore;

namespace BrydonServer.Auth;

public class UserStore(AppDbContext db)
{
    public Task<User?> FindByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
}
