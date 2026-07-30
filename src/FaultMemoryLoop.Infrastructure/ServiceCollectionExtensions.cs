using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Infrastructure.AuthServices;
using FaultMemoryLoop.Infrastructure.Persistence;
using FaultMemoryLoop.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FaultMemoryLoop.Infrastructure;

/// <summary>
/// Wires authentication into DI — both login paths. Program.cs calls this
/// one method rather than knowing about JWT signing details, Google's
/// verification library, or the database connection string directly.
///
/// AI services are deliberately not registered here yet — that's a separate
/// commit, on hold for now (see docs/design.md).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        string jwtSigningKey,
        string jwtIssuer,
        string jwtAudience,
        string googleClientId,
        string sqliteConnectionString)
    {
        services.AddSingleton<ITokenService>(_ =>
            new JwtTokenService(jwtSigningKey, jwtIssuer, jwtAudience));

        services.AddSingleton<IGoogleTokenVerifier>(_ =>
            new GoogleTokenVerifier(googleClientId));

        services.AddDbContext<FaultMemoryLoopDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
