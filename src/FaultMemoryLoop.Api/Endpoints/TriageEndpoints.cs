using System.Security.Claims;
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
            IRetrievalService retrievalService,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return Results.Ok(ApiResponse<object>.Fail(errors));
            }

            var triage = await extractionService.ExtractAsync(
                request.RawDescription, request.Vehicle, request.CreatedBy, ct);

            var suggestion = await retrievalService.FindSimilarAsync(triage, ct);

            return Results.Ok(ApiResponse<TriageResponse>.Ok(new TriageResponse(triage, suggestion)));
        })
        .WithName("SubmitTriage")
        .WithSummary("Submit a customer's raw fault description for triage.")
        .Produces<ApiResponse<TriageResponse>>(StatusCodes.Status200OK);
    }
}