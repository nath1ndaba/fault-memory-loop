// Evaluation harness — scores the triage + retrieval pipeline against
// eval/test-cases/cases.json on three axes:
//   1. Retrieval precision   — did it find the right precedent, if one existed
//   2. Hallucination rate    — did any suggestion claim something the cited
//                              jobs don't actually support
//   3. Abstention correctness — did it correctly say "no precedent" when
//                              there wasn't one, instead of guessing
//
// NEXT STEP: this currently just loads and prints the test cases. Once the
// real extraction + retrieval services are wired up in FaultMemoryLoop.Api,
// this harness should call them directly (as a referenced project, or over
// HTTP against a running instance) and score the actual output against the
// `expected` block in each test case.

using System.Text.Json;

var testCasesPath = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "eval", "test-cases", "cases.json");

if (!File.Exists(testCasesPath))
{
    Console.WriteLine($"Test cases not found at {testCasesPath}");
    return;
}

var json = await File.ReadAllTextAsync(testCasesPath);
var cases = JsonDocument.Parse(json).RootElement;

Console.WriteLine($"Loaded {cases.GetArrayLength()} test cases.");
Console.WriteLine("(Scoring not yet implemented — see NEXT STEP comment above.)");

foreach (var testCase in cases.EnumerateArray())
{
    var id = testCase.GetProperty("id").GetString();
    var description = testCase.GetProperty("rawDescription").GetString();
    Console.WriteLine($"- {id}: {description}");
}
