using FaultMemoryLoop.Domain.Common;

namespace FaultMemoryLoop.Domain.Entities;

/// <summary>
/// An adviser who can log in with an email/password, as an alternative to
/// Google sign-in. PasswordHash is a PBKDF2 hash + salt + iteration count,
/// never a plaintext password — see Pbkdf2PasswordHasher in Infrastructure.
/// </summary>
public record Employee(
    Guid Id,
    string Email,
    string PasswordHash,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy) : AuditableEntity(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
