namespace FaultMemoryLoop.Api.Endpoints;

/// <summary>
/// The only endpoint in this commit. Proves the pipeline (logging, rate
/// limiting, API docs) actually runs end to end. Real endpoints — triage,
/// then auth — land in their own commits on top of this skeleton.
/// </summary>
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithTags("Health")
            .WithName("HealthCheck");
    }
}
