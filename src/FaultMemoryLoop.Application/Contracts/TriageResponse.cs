using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.Models;

namespace FaultMemoryLoop.Application.Contracts;

public record TriageResponse(TriageRecord Triage, RetrievalSuggestion Suggestion);