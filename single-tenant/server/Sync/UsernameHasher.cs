using System.Security.Cryptography;
using System.Text;

namespace BrydonServer.Sync;

// Deterministic (unlike PasswordHasher's random-salt hash) so patient-portal
// can match the same username reported by different tenants, or later look up
// a username a patient types by hashing it the same way. Keyed with a shared
// secret so the directory can't be trivially reversed by dictionary/rainbow
// lookup if patient-portal's database were exposed.
public static class UsernameHasher
{
    public static string Hash(string username, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(username.ToLowerInvariant()));
        return Convert.ToHexString(hash);
    }
}
