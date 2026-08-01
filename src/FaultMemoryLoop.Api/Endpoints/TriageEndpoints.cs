using FaultMemoryLoop.Application.Contracts;
using FaultMemoryLoop.Application.Interfaces;
using FluentValidation;

namespace FaultMemoryLoop.Api.Endpoints;

public static class TriageEndpoints
{
    public static void MapTriageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/triage")
            .WithTags("Triage")
            .RequireRateLimiting("triage")
            .RequireAuthorization();

        group.MapPost("/", async (
            TriageRequest request,
            IValidator<TriageRequest> validator,
            ITriageExtractionService extractionService,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return Results.Ok(ApiResponse<object>.Fail(errors));
            }

            var record = await extractionService.ExtractAsync(
                request.RawDescription, request.Vehicle, request.CreatedBy, ct);

            return Results.Ok(ApiResponse<object>.Ok(record));
        })
        .WithName("SubmitTriage")
        .WithSummary("Submit a customer's raw fault description for triage.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);
    }
}
