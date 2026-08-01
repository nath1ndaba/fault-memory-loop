using System.Text.Json;
using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.Enums;
using FaultMemoryLoop.Domain.ValueObjects;
using Microsoft.Extensions.AI;

namespace FaultMemoryLoop.Infrastructure.AiServices;

/// <summary>
/// Extracts a TriageRecord using Gemini via the Microsoft.Extensions.AI
/// abstraction (IChatClient). The client is injected, so swapping providers
/// later (OpenAI, Azure OpenAI, Anthropic) is a DI registration change in
/// Program.cs, not a rewrite of this class.
///
/// The model is asked to return JSON matching a narrow extraction shape.
/// Id, CreatedAt, and CreatedBy are deliberately assigned here in code, not
/// taken from the model's response — the model structures what the customer
/// said, it never assigns system-owned identifiers.
/// </summary>
public class GeminiTriageExtractionService(IChatClient chatClient) : ITriageExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TriageRecord> ExtractAsync(
        string rawDescription,
        VehicleInfo? vehicle,
        string createdBy,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(rawDescription, vehicle);

        var response = await chatClient.GetResponseAsync(prompt, cancellationToken: ct);

        var jsonText = ExtractJson(response.Text);
        var extraction = JsonSerializer.Deserialize<ExtractionDto>(jsonText, JsonOptions)
            ?? throw new InvalidOperationException("Model returned no parseable extraction.");

        return new TriageRecord(
            Id: Guid.NewGuid(),
            RawDescription: rawDescription,
            Vehicle: vehicle,
            System: ParseSystem(extraction.System),
            FaultCategory: extraction.FaultCategory ?? "Unclassified",
            SymptomTags: extraction.SymptomTags ?? [],
            Urgency: ParseUrgency(extraction.Urgency),
            ClarifyingQuestions: extraction.ClarifyingQuestions ?? [],
            ExtractionConfidence: extraction.ExtractionConfidence ?? 0.0,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: createdBy);
    }

    private static string BuildPrompt(string rawDescription, VehicleInfo? vehicle)
    {
        var vehicleContext = vehicle is null
            ? "No vehicle details provided."
            : $"Vehicle: {vehicle.Make} {vehicle.Model} ({vehicle.Year}), mileage {vehicle.Mileage}.";

        return $$"""
            You are triaging a customer's free-text description of a vehicle
            fault for a garage service adviser. Structure what the customer
            said — do not diagnose or guess the underlying cause.

            {{vehicleContext}}
            Customer's description: "{{rawDescription}}"

            Respond with ONLY a JSON object, no other text, matching exactly:
            {
              "system": one of Engine, Transmission, Brakes, Suspension, Steering, Electrical, Hvac, Exhaust, Tyres, Bodywork, Unknown,
              "faultCategory": a short free-text label, e.g. "CV joint / driveshaft",
              "symptomTags": an array of short kebab-case tags, e.g. ["clicking-noise", "pulls-left"],
              "urgency": one of Low, Medium, High, SafetyCritical — use SafetyCritical for anything touching brakes or steering,
              "clarifyingQuestions": an array of 1-3 questions the adviser could ask the customer right now,
              "extractionConfidence": a number 0.0-1.0 for how confident you are in this structuring of the input
            }
            """;
    }
    /// <summary>
    /// Models sometimes wrap JSON in markdown fences or add stray text
    /// despite instructions not to. Strips down to the first { through the
    /// last } as a pragmatic defense, since we're relying on prompt
    /// instruction rather than provider-enforced schema (GeminiDotnet's
    /// mapper doesn't support the plain JSON response-format mode).
    /// </summary>
    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end < start)
        {
            throw new InvalidOperationException($"Could not locate a JSON object in model response: {text}");
        }
        return text[start..(end + 1)];
    }
    private static VehicleSystem ParseSystem(string? value) =>
        Enum.TryParse<VehicleSystem>(value, ignoreCase: true, out var result) ? result : VehicleSystem.Unknown;

    private static Urgency ParseUrgency(string? value) =>
        Enum.TryParse<Urgency>(value, ignoreCase: true, out var result) ? result : Urgency.Medium;

    /// <summary>Shape of the model's JSON response, before mapping into the domain entity.</summary>
    private sealed class ExtractionDto
    {
        public string? System { get; set; }
        public string? FaultCategory { get; set; }
        public List<string>? SymptomTags { get; set; }
        public string? Urgency { get; set; }
        public List<string>? ClarifyingQuestions { get; set; }
        public double? ExtractionConfidence { get; set; }
    }
}
