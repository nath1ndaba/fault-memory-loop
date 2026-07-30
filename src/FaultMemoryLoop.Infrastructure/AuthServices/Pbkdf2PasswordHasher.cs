using System.Security.Cryptography;
using FaultMemoryLoop.Application.Interfaces;

namespace FaultMemoryLoop.Infrastructure.AuthServices;

/// <summary>
/// Real PBKDF2 password hashing via .NET's built-in
/// System.Security.Cryptography.Rfc2898DeriveBytes — no extra package
/// needed, and no guessed dependency version. Salt and iteration count are
/// stored alongside the hash so the work factor can change over time
/// without invalidating existing hashes.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000; // OWASP-recommended minimum for PBKDF2-SHA256 as of 2023+

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
