using LinkShortener.Domain;

namespace LinkShortener.Application.Abstractions;

public interface ILinkRepository
{
    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default);
    Task<int> CountRecentCreationAttemptsAsync(string sourceAddress, DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<ShortLink?> CreateAsync(ShortLink link, string sourceAddress, CancellationToken cancellationToken = default);
    Task<ShortLink?> IncrementAccessCountIfActiveAsync(string code, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ShortLink?> GetStatsAsync(string code, CancellationToken cancellationToken = default);
}
