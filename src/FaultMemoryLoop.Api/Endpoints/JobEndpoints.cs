using FaultMemoryLoop.Application.Contracts;
using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FluentValidation;

namespace FaultMemoryLoop.Api.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs/resolve", async (
            ResolveJobRequest request,
            IValidator<ResolveJobRequest> validator,
            IJobRecordRepository jobRepository,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return Results.Ok(ApiResponse<object>.Fail(
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var now = DateTimeOffset.UtcNow;
            var record = new ResolvedJobRecord(
                Id: Guid.NewGuid(),
                CreatedAt: now,
                CreatedBy: request.ResolvedBy,
                UpdatedAt: now,
                UpdatedBy: request.ResolvedBy,
                Vehicle: request.Vehicle,
                OriginalTriageId: request.OriginalTriageId,
                System: request.System,
                SymptomTags: request.SymptomTags,
                ActualDiagnosis: request.ActualDiagnosis,
                ActualFix: request.ActualFix,
                PartsUsed: request.PartsUsed,
                LabourHours: request.LabourHours,
                OutcomeConfirmed: request.OutcomeConfirmed);

            await jobRepository.AddAsync(record, ct);
            return Results.Ok(ApiResponse<object>.Ok(new { record.Id }));
        })
        .WithTags("Jobs")
        .RequireAuthorization()
        .WithName("ResolveJob")
        .WithSummary("Record a technician's confirmed diagnosis and fix, growing the knowledge store.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);
    }
}