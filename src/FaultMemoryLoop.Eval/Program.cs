using System.Text.Json;
using DotNetEnv;
using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Enums;
using FaultMemoryLoop.Domain.ValueObjects;
using FaultMemoryLoop.Infrastructure.AiServices;
using FaultMemoryLoop.Infrastructure.Repositories;
using FaultMemoryLoop.Infrastructure.Retrieval;
using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;

// Evaluation harness — scores the REAL triage + retrieval pipeline (the
// same GeminiTriageExtractionService and TagOverlapRetrievalService the API
// uses, not a mock) against eval/test-cases/cases.json, on the three axes
// committed to in docs/design.md:
//   1. Retrieval precision    — did it find the right precedent, if one existed
//   2. Abstention correctness — did it correctly say "no precedent" when
//                               there wasn't one, instead of guessing
//   3. Hallucination rate     — did any suggestion claim a fix not backed by
//                               a cited past job

var repoRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
Env.Load(Path.Combine(repoRoot, "src", "FaultMemoryLoop.Api", ".env"));

var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("GEMINI_API_KEY not set — copy .env.example to .env at the repo root.");
var geminiModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-3.6-flash";

IChatClient chatClient = new GeminiChatClient(new GeminiClientOptions { ApiKey = geminiApiKey, ModelId = geminiModel });
ITriageExtractionService extractionService = new GeminiTriageExtractionService(chatClient);

var knowledgeStorePath = Path.Combine(repoRoot, "knowledge-store", "jobs");
IJobRecordRepository jobRepository = new MarkdownJobRecordRepository(knowledgeStorePath);
IRetrievalService retrievalService = new TagOverlapRetrievalService(jobRepository);

var testCasesPath = Path.Combine(repoRoot, "eval", "test-cases", "cases.json");
if (!File.Exists(testCasesPath))
{
    Console.WriteLine($"Test cases not found at {testCasesPath}");
    return;
}

var json = await File.ReadAllTextAsync(testCasesPath);
var cases = JsonSerializer.Deserialize<List<TestCase>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
    ?? throw new InvalidOperationException("Could not parse test cases.");

Console.WriteLine($"Loaded {cases.Count} test cases.\n");

var results = new List<CaseResult>();

foreach (var testCase in cases)
{
    Console.WriteLine($"--- {testCase.Id} ---");
    Console.WriteLine($"Input: {testCase.RawDescription}");

    var vehicle = testCase.Vehicle is null
        ? null
        : new VehicleInfo(testCase.Vehicle.Make, testCase.Vehicle.Model, testCase.Vehicle.Year, null);

    var triage = await extractionService.ExtractAsync(testCase.RawDescription, vehicle, "eval-harness");
    var suggestion = await retrievalService.FindSimilarAsync(triage);

    var result = new CaseResult(testCase.Id);

    // --- System classification (informational — not one of the three core metrics) ---
    if (testCase.Expected.System is not null)
    {
        var expectedSystem = Enum.Parse<VehicleSystem>(testCase.Expected.System, ignoreCase: true);
        result.SystemCorrect = triage.System == expectedSystem;
        Console.WriteLine($"  System: got {triage.System}, expected {expectedSystem} — {(result.SystemCorrect == true ? "OK" : "MISMATCH")}");
    }

    // --- Urgency (informational) ---
    if (testCase.Expected.Urgency is not null)
    {
        var expectedUrgency = Enum.Parse<Urgency>(testCase.Expected.Urgency, ignoreCase: true);
        result.UrgencyCorrect = triage.Urgency == expectedUrgency;
        Console.WriteLine($"  Urgency: got {triage.Urgency}, expected {expectedUrgency} — {(result.UrgencyCorrect == true ? "OK" : "MISMATCH")}");
    }

    // --- Retrieval precision + abstention correctness ---
    if (testCase.Expected.ShouldMatchPrecedent)
    {
        result.IsRetrievalCase = true;
        result.RetrievalCorrect = suggestion.MatchFound;

        if (suggestion.MatchFound && testCase.Expected.ExpectedMatchedJobId is not null)
        {
            var expectedId = Guid.Parse(testCase.Expected.ExpectedMatchedJobId);
            result.RetrievalCorrect &= suggestion.MatchedJobId == expectedId;
        }

        if (suggestion.MatchFound && testCase.Expected.MinSimilarity is not null)
        {
            result.RetrievalCorrect &= suggestion.SimilarityScore >= testCase.Expected.MinSimilarity;
        }

        Console.WriteLine($"  Retrieval: matchFound={suggestion.MatchFound}, expected a match — {(result.RetrievalCorrect == true ? "OK" : "MISS")}");
    }
    else
    {
        result.IsAbstentionCase = true;
        result.AbstentionCorrect = !suggestion.MatchFound;
        Console.WriteLine($"  Abstention: matchFound={suggestion.MatchFound}, expected no match — {(result.AbstentionCorrect == true ? "OK" : "FALSE POSITIVE")}");
    }

    // --- Hallucination check ---
    // This system can only ever recommend a fix pulled directly from a
    // cited past job's ActualFix — there's no free-text "recommended fix"
    // generated independent of retrieved evidence (see
    // TagOverlapRetrievalService). So MatchFound=true with zero citations
    // would be a genuine bug, not just a bad test case — checked here as a
    // safety net.
    result.Hallucinated = suggestion.MatchFound && suggestion.CitedJobIds.Count == 0;
    if (result.Hallucinated == true)
    {
        Console.WriteLine("  ⚠ HALLUCINATION: matchFound=true but no cited job IDs.");
    }

    if (testCase.Expected.Note is not null)
    {
        Console.WriteLine($"  Note: {testCase.Expected.Note}");
    }

    Console.WriteLine();
    results.Add(result);
}

// --- Aggregate the three metrics committed to in docs/design.md ---
var retrievalCases = results.Where(r => r.IsRetrievalCase).ToList();
var abstentionCases = results.Where(r => r.IsAbstentionCase).ToList();

var retrievalPrecision = retrievalCases.Count == 0 ? (double?)null
    : (double)retrievalCases.Count(r => r.RetrievalCorrect == true) / retrievalCases.Count;

var abstentionCorrectness = abstentionCases.Count == 0 ? (double?)null
    : (double)abstentionCases.Count(r => r.AbstentionCorrect == true) / abstentionCases.Count;

var hallucinationRate = (double)results.Count(r => r.Hallucinated == true) / results.Count;

Console.WriteLine("=== Summary ===");
Console.WriteLine($"Retrieval precision:    {(retrievalPrecision is null ? "n/a (no retrieval cases)" : $"{retrievalPrecision:P0} ({retrievalCases.Count(r => r.RetrievalCorrect == true)}/{retrievalCases.Count})")}");
Console.WriteLine($"Abstention correctness: {(abstentionCorrectness is null ? "n/a (no abstention cases)" : $"{abstentionCorrectness:P0} ({abstentionCases.Count(r => r.AbstentionCorrect == true)}/{abstentionCases.Count})")}");
Console.WriteLine($"Hallucination rate:     {hallucinationRate:P0} ({results.Count(r => r.Hallucinated == true)}/{results.Count})");

var systemCases = results.Where(r => r.SystemCorrect is not null).ToList();
if (systemCases.Count > 0)
{
    var systemAccuracy = (double)systemCases.Count(r => r.SystemCorrect == true) / systemCases.Count;
    Console.WriteLine($"(Informational) System classification accuracy: {systemAccuracy:P0} ({systemCases.Count(r => r.SystemCorrect == true)}/{systemCases.Count})");
}

record TestCase(string Id, string RawDescription, TestVehicle? Vehicle, ExpectedResult Expected);
record TestVehicle(string? Make, string? Model, int? Year);
record ExpectedResult(string? System, string? Urgency, bool ShouldMatchPrecedent, string? ExpectedMatchedJobId, double? MinSimilarity, string? Note);

class CaseResult(string id)
{
    public string Id { get; } = id;
    public bool? SystemCorrect { get; set; }
    public bool? UrgencyCorrect { get; set; }
    public bool IsRetrievalCase { get; set; }
    public bool? RetrievalCorrect { get; set; }
    public bool IsAbstentionCase { get; set; }
    public bool? AbstentionCorrect { get; set; }
    public bool? Hallucinated { get; set; }
}