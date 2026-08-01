using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using FaultMemoryLoop.Api.Endpoints;
using FaultMemoryLoop.Application.Contracts;
using FaultMemoryLoop.Application.Validators;
using FaultMemoryLoop.Infrastructure;
using FaultMemoryLoop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// Load .env for local development. In real deployments (Render, etc.), these
// would come from the platform's environment/secrets config instead — this
// call is a harmless no-op if no .env file exists.
Env.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// --- Rate limiting -------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("triage", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// --- Validation ----------------------------------------------------------
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<TriageRequest>, TriageRequestValidator>();
builder.Services.AddScoped<IValidator<ResolveJobRequest>, ResolveJobRequestValidator>();

// --- Auth configuration ---------------------------------------------------
var jwtSigningKey = builder.Configuration["JWT_SIGNING_KEY"]
    ?? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
    ?? throw new InvalidOperationException(
        "JWT_SIGNING_KEY is not set. Copy .env.example to .env and add one (32+ characters).");

var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"]
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
    ?? throw new InvalidOperationException(
        "GOOGLE_CLIENT_ID is not set. Copy .env.example to .env and add your Google OAuth Client ID.");

var sqliteConnectionString = builder.Configuration["SQLITE_CONNECTION_STRING"]
    ?? Environment.GetEnvironmentVariable("SQLITE_CONNECTION_STRING")
    ?? "Data Source=faultmemoryloop.db";

const string jwtIssuer = "FaultMemoryLoop";
const string jwtAudience = "FaultMemoryLoop.Api";

builder.Services.AddAuthenticationServices(
    jwtSigningKey, jwtIssuer, jwtAudience, googleClientId, sqliteConnectionString);

// --- AI configuration ------------------------------------------------------
var geminiApiKey = builder.Configuration["GEMINI_API_KEY"]
    ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException(
        "GEMINI_API_KEY is not set. Copy .env.example to .env and add your key.");

var geminiModel = builder.Configuration["GEMINI_MODEL"]
    ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
    ?? "gemini-3.6-flash";

var knowledgeStorePath = Path.Combine(
    builder.Environment.ContentRootPath, "..", "..", "knowledge-store", "jobs");

builder.Services.AddAiServices(geminiApiKey, geminiModel, knowledgeStorePath);

// Real JWT validation — not a stand-in. Only tokens this system itself
// issued (via /api/auth/google or /api/auth/login) will pass this check.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending EF Core migrations at startup. If none exist yet (see
// src/FaultMemoryLoop.Infrastructure/Migrations/README.md), this is a
// harmless no-op until `dotnet ef migrations add InitialCreate` is run.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FaultMemoryLoopDbContext>();
    dbContext.Database.Migrate();
}

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(); // serves interactive docs at /scalar

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapTriageEndpoints();
app.MapJobEndpoints();

// Auto-open the Scalar docs after a successful local launch.
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"{url}/scalar",
                UseShellExecute = true
            });
        }
        catch
        {
            Log.Information("Open {Url}/scalar to view the API docs.", url);
        }
    });
}

app.Run();
