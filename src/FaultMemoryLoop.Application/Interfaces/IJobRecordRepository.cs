using FaultMemoryLoop.Domain.Entities;

namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// The port for the knowledge store. This is deliberately narrow and specific
/// to ResolvedJobRecord — not a generic IRepository&lt;T&gt; base abstraction,
/// which was scoped out of this build (see docs/design.md, "path to
/// production") because it earns its complexity once there's a real
/// database behind more than one entity type. This interface is what's
/// actually needed today: the Markdown-backed store behind it is a real,
/// working implementation, not a stand-in for one.
/// </summary>
public interface IJobRecordRepository
{
    Task<ResolvedJobRecord> AddAsync(ResolvedJobRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ResolvedJobRecord>> GetAllAsync(CancellationToken ct = default);
    Task<ResolvedJobRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
