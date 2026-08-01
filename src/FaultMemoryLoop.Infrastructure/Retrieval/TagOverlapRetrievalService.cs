using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.Models;

namespace FaultMemoryLoop.Infrastructure.Retrieval;

/// <summary>
/// A deliberately simple first pass at retrieval: scores past resolved jobs
/// by symptom-tag overlap, gated by matching vehicle system. This is NOT
/// semantic/embedding-based similarity — that's a real upgrade path (see
/// docs/design.md) — but it's honest and it works: two fault descriptions
/// with genuinely overlapping symptom tags on the same vehicle system are a
/// reasonable signal, and it never fabricates a match where the overlap is
/// weak or absent.
/// </summary>
public class TagOverlapRetrievalService(IJobRecordRepository jobRepository) : IRetrievalService
{
    private const double MatchThreshold = 0.5;

    public async Task<RetrievalSuggestion> FindSimilarAsync(TriageRecord triage, CancellationToken ct = default)
    {
        var pastJobs = await jobRepository.GetAllAsync(ct);

        // Retrieval only trusts jobs a technician actually confirmed fixed —
        // see docs/schema.md on why OutcomeConfirmed exists.
        var confirmedJobs = pastJobs.Where(j => j.OutcomeConfirmed).ToList();

        var scored = confirmedJobs
            .Select(job => (Job: job, Score: ScoreSimilarity(triage, job)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0 || scored[0].Score < MatchThreshold)
        {
            return new RetrievalSuggestion(
                MatchFound: false,
                MatchedJobId: null,
                MatchedFaultSummary: null,
                ConfirmedFix: null,
                SimilarityScore: null,
                SimilarPastCaseCount: 0,
                Recommendation: "No strong precedent found in the knowledge store — standard diagnostic path applies.",
                CitedJobIds: []);
        }

        var best = scored[0];
        var citedJobIds = scored.Where(x => x.Score >= MatchThreshold).Select(x => x.Job.Id).ToList();

        return new RetrievalSuggestion(
            MatchFound: true,
            MatchedJobId: best.Job.Id,
            MatchedFaultSummary: best.Job.ActualDiagnosis,
            ConfirmedFix: best.Job.ActualFix,
            SimilarityScore: best.Score,
            SimilarPastCaseCount: citedJobIds.Count,
            Recommendation: $"Similar to {citedJobIds.Count} past confirmed case(s): {best.Job.ActualDiagnosis}, fixed by: {best.Job.ActualFix}.",
            CitedJobIds: citedJobIds);
    }

    /// <summary>
    /// Jaccard tag overlap (intersection / union of symptom tags), gated to
    /// zero if the vehicle system doesn't match — a suspension fault should
    /// never match an electrical job no matter how the tags happen to overlap.
    /// </summary>
    private static double ScoreSimilarity(TriageRecord triage, ResolvedJobRecord job)
    {
        if (job.System != triage.System || triage.SymptomTags.Count == 0)
        {
            return 0;
        }

        var triageTags = triage.SymptomTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var jobTags = job.SymptomTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var intersection = triageTags.Intersect(jobTags, StringComparer.OrdinalIgnoreCase).Count();
        var union = triageTags.Union(jobTags, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }
}