using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Infrastructure.AiServices;
using FaultMemoryLoop.Infrastructure.AuthServices;
using FaultMemoryLoop.Infrastructure.Persistence;
using FaultMemoryLoop.Infrastructure.Repositories;
using FaultMemoryLoop.Infrastructure.Retrieval;
using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace FaultMemoryLoop.Infrastructure;

/// <summary>
/// One extension method per concern, mirroring how Program.cs is organised —
/// each method wires exactly one feature area into DI, and vendor-specific
/// types (Gemini, Google, JWT signing details, the SQLite connection
/// string) stay encapsulated here rather than leaking into the composition
/// root.
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

    public static IServiceCollection AddAiServices(
    this IServiceCollection services, string geminiApiKey, string geminiModel, string knowledgeStorePath)
    {
        services.AddSingleton<IChatClient>(_ =>
            new GeminiChatClient(new GeminiClientOptions { ApiKey = geminiApiKey, ModelId = geminiModel }));

        services.AddScoped<ITriageExtractionService, GeminiTriageExtractionService>();

        services.AddSingleton<IJobRecordRepository>(_ =>
            new MarkdownJobRecordRepository(knowledgeStorePath));

        services.AddScoped<IRetrievalService, TagOverlapRetrievalService>();

        return services;
    }

}
