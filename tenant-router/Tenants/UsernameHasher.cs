using System.Security.Cryptography;
using System.Text;

namespace TenantRouter.Tenants;

// Must stay byte-for-byte identical to single-tenant's Sync/UsernameHasher.cs
// (independently deployable services, no shared library between them). Both
// hash with the same shared TENANT_REPORT_SECRET, so a username a caller
// looks up hashes to the same value a tenant reported for that user.
public static class UsernameHasher
{
    public static string Hash(string username, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(username.ToLowerInvariant()));
        return Convert.ToHexString(hash);
    }
}
