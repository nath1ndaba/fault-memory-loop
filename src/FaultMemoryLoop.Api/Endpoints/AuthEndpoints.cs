using System.Security.Claims;
using FaultMemoryLoop.Application.Contracts;
using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FluentValidation;

namespace FaultMemoryLoop.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // --- Option 1: Google sign-in --------------------------------------
        group.MapPost("/google", async (
            GoogleAuthRequest request,
            IGoogleTokenVerifier googleVerifier,
            ITokenService tokenService,
            CancellationToken ct) =>
        {
            var identity = await googleVerifier.VerifyAsync(request.IdToken, ct);
            if (identity is null)
            {
                return Results.Ok(ApiResponse<object>.Fail("Invalid or expired Google ID token."));
            }

            var (token, expiresAt) = tokenService.GenerateToken(identity.Subject, identity.Email);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(token, expiresAt, identity.Email)));
        })
        .WithName("GoogleSignIn")
        .WithSummary("Option 1: exchange a Google ID token for this system's own bearer token.")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK);

        // --- Option 2: email + password -------------------------------------
        group.MapPost("/register", async (
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            IEmployeeRepository employees,
            IPasswordHasher hasher,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return Results.Ok(ApiResponse<object>.Fail(
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
            }

            if (await employees.GetByEmailAsync(request.Email, ct) is not null)
            {
                return Results.Ok(ApiResponse<object>.Fail("An account with this email already exists."));
            }

            var now = DateTimeOffset.UtcNow;
            var employee = new Employee(
                Id: Guid.NewGuid(),
                Email: request.Email,
                PasswordHash: hasher.Hash(request.Password),
                CreatedAt: now,
                CreatedBy: request.Email,
                UpdatedAt: now,
                UpdatedBy: request.Email);

            await employees.AddAsync(employee, ct);
            return Results.Ok(ApiResponse<object>.Ok(new { employee.Id, employee.Email }));
        })
        .WithName("Register")
        .WithSummary("Option 2, step 1: create an employee account with email + password.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            IEmployeeRepository employees,
            IPasswordHasher hasher,
            ITokenService tokenService,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return Results.Ok(ApiResponse<object>.Fail(
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var employee = await employees.GetByEmailAsync(request.Email, ct);
            if (employee is null || !hasher.Verify(request.Password, employee.PasswordHash))
            {
                return Results.Ok(ApiResponse<object>.Fail("Invalid email or password."));
            }

            var (token, expiresAt) = tokenService.GenerateToken(employee.Id.ToString(), employee.Email);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(token, expiresAt, employee.Email)));
        })
        .WithName("Login")
        .WithSummary("Option 2, step 2: log in with email + password.")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK);

        // Proves the whole chain works end to end regardless of which login
        // option was used — both issue the same shape of token.
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Results.Ok(ApiResponse<object>.Ok(new { subject, email }));
        })
        .RequireAuthorization()
        .WithName("WhoAmI")
        .WithSummary("Returns the identity encoded in the presented bearer token.");
    }
}
