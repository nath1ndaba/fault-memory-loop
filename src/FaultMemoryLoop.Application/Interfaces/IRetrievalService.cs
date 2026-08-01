using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.Models;

namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// Searches the knowledge store for past resolved jobs similar to a new
/// triage result. "No match" is a first-class, honest outcome — see
/// docs/schema.md for why this matters more than finding a match at all.
/// </summary>
public interface IRetrievalService
{
    Task<RetrievalSuggestion> FindSimilarAsync(TriageRecord triage, CancellationToken ct = default);
}