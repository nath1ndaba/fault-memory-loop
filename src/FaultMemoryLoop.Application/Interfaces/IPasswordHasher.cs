namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// Hashes and verifies passwords. Kept as an interface so Infrastructure's
/// choice of algorithm (PBKDF2) is swappable without touching anything
/// that depends on this contract.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
