using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Infrastructure.Repositories;

/// <summary>
/// Stores each ResolvedJobRecord as a Markdown file with YAML frontmatter
/// under knowledge-store/jobs/ — human-readable documentation and
/// machine-retrievable memory in the same file, per docs/design.md.
///
/// This is a genuine, working implementation, not a stand-in for a future
/// database — the generic IRepository&lt;T&gt; abstraction was deferred (see
/// docs/design.md) because it earns its cost once there's a real database
/// behind more than one entity type. There isn't yet, so this talks to
/// files directly.
/// </summary>
public class MarkdownJobRecordRepository(string knowledgeStorePath) : IJobRecordRepository
{
    public async Task<ResolvedJobRecord> AddAsync(ResolvedJobRecord record, CancellationToken ct = default)
    {
        Directory.CreateDirectory(knowledgeStorePath);
        var path = Path.Combine(knowledgeStorePath, $"job-{record.Id}.md");
        await File.WriteAllTextAsync(path, ToMarkdown(record), ct);
        return record;
    }

    public async Task<IReadOnlyList<ResolvedJobRecord>> GetAllAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(knowledgeStorePath))
        {
            return [];
        }

        var records = new List<ResolvedJobRecord>();
        foreach (var file in Directory.EnumerateFiles(knowledgeStorePath, "*.md"))
        {
            var content = await File.ReadAllTextAsync(file, ct);
            if (TryParse(content, out var record))
            {
                records.Add(record);
            }
        }

        return records;
    }

    public async Task<ResolvedJobRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(r => r.Id == id);
    }

    private static string ToMarkdown(ResolvedJobRecord record) => $"""
        ---
        id: {record.Id}
        createdAt: {record.CreatedAt:O}
        createdBy: {record.CreatedBy}
        updatedAt: {record.UpdatedAt:O}
        updatedBy: {record.UpdatedBy}
        vehicle:
          make: {record.Vehicle.Make}
          model: {record.Vehicle.Model}
          year: {record.Vehicle.Year}
        originalTriage: {record.OriginalTriageId}
        actualDiagnosis: {record.ActualDiagnosis}
        actualFix: {record.ActualFix}
        partsUsed: [{string.Join(", ", record.PartsUsed)}]
        labourHours: {record.LabourHours}
        outcomeConfirmed: {record.OutcomeConfirmed.ToString().ToLowerInvariant()}
        ---
        """;

    // NEXT STEP: this parses only the frontmatter fields needed to
    // reconstruct a ResolvedJobRecord. A real YAML parser (e.g. YamlDotNet)
    // would be more robust than hand-rolled parsing once frontmatter grows
    // more complex — left simple for now since the shape is still small and
    // fully controlled by ToMarkdown above.
    private static bool TryParse(string content, out ResolvedJobRecord record)
    {
        record = null!;
        try
        {
            var lines = content.Split('\n');
            var map = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0) continue;
                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                map[key] = value;
            }

            record = new ResolvedJobRecord(
                Id: Guid.Parse(map["id"]),
                CreatedAt: DateTimeOffset.Parse(map["createdAt"]),
                CreatedBy: map["createdBy"],
                UpdatedAt: DateTimeOffset.Parse(map["updatedAt"]),
                UpdatedBy: map["updatedBy"],
                Vehicle: new VehicleInfo(
                    map.GetValueOrDefault("make"),
                    map.GetValueOrDefault("model"),
                    int.TryParse(map.GetValueOrDefault("year"), out var year) ? year : null,
                    null),
                OriginalTriageId: Guid.Parse(map["originalTriage"]),
                ActualDiagnosis: map["actualDiagnosis"],
                ActualFix: map["actualFix"],
                PartsUsed: [],
                LabourHours: double.TryParse(map.GetValueOrDefault("labourHours"), out var hours) ? hours : 0,
                OutcomeConfirmed: map.GetValueOrDefault("outcomeConfirmed") == "true");

            return true;
        }
        catch
        {
            return false;
        }
    }
}
