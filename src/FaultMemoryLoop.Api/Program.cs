using System.Threading.RateLimiting;
using FaultMemoryLoop.Api.Endpoints;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Rate limiting is wired now, ahead of any feature, since it's a pipeline
// concern rather than something tied to a specific endpoint. No endpoint
// uses the "triage" policy yet — it'll be attached once that endpoint exists.
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

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference(); // serves interactive docs at /scalar

app.MapHealthEndpoints();

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
