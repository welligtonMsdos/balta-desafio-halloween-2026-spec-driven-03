using LinkShortener.Application.Abstractions;
using LinkShortener.Domain;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Infrastructure.Persistence;

public sealed class LinkRepository : ILinkRepository
{
    private readonly LinkShortenerDbContext _dbContext;

    public LinkRepository(LinkShortenerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShortLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.Code == code, cancellationToken);
    }

    public async Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShortLinks.AnyAsync(link => link.Code == code, cancellationToken);
    }

    public async Task<int> CountRecentCreationAttemptsAsync(string sourceAddress, DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LinkCreationAttempts
            .CountAsync(attempt => attempt.SourceAddress == sourceAddress && attempt.CreatedAtUtc >= cutoffUtc, cancellationToken);
    }

    public async Task<ShortLink?> CreateAsync(ShortLink link, string sourceAddress, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var attempt = new LinkCreationAttempt
        {
            Id = Guid.NewGuid(),
            SourceAddress = sourceAddress,
            CreatedAtUtc = now
        };

        try
        {
            _dbContext.ShortLinks.Add(link);
            _dbContext.LinkCreationAttempts.Add(attempt);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return link;
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public async Task<ShortLink?> IncrementAccessCountIfActiveAsync(string code, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.ShortLinks
            .Where(link => link.Code == code && (!link.ExpiresAtUtc.HasValue || link.ExpiresAtUtc.Value > utcNow))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(link => link.AccessCount, link => link.AccessCount + 1),
                cancellationToken);

        if (affected != 1)
        {
            return null;
        }

        return await _dbContext.ShortLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.Code == code, cancellationToken);
    }

    public async Task<ShortLink?> GetStatsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShortLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.Code == code, cancellationToken);
    }
}
