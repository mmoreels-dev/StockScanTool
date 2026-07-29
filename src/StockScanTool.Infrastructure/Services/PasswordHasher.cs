using System.Security.Cryptography;

namespace StockScanTool.Infrastructure.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Convert.ToBase64String(hash)}.{Convert.ToBase64String(salt)}";
    }

    public static bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2) return false;

        var hash = Convert.FromBase64String(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var testHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return CryptographicOperations.FixedTimeEquals(hash, testHash);
    }
}
