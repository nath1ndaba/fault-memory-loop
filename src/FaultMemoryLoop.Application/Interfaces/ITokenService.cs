namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// Issues access tokens for an authenticated identity. Kept as an interface
/// so the concrete signing mechanism (Infrastructure) is swappable without
/// touching anything that depends on this contract.
/// </summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(string subjectId, string email);
}
