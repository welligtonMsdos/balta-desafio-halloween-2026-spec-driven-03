using LinkShortener.Application;
using LinkShortener.Application.Abstractions;
using LinkShortener.Domain;

namespace LinkShortener.UnitTests;

public sealed class LinkServiceTests
{
    [Fact]
    public async Task CreateLinkAsync_WithValidUrl_ReturnsShortLink()
    {
        var repository = new StubLinkRepository();
        var service = new LinkService(repository, new FakeClock(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)));

        var result = await service.CreateLinkAsync(new CreateLinkRequest("https://example.com/long/path", null, null), "127.0.0.1");

        Assert.Equal(7, result.Code.Length);
        Assert.Equal("HTTP://LOCALHOST:8080/" + result.Code, result.ShortUrl.ToUpperInvariant());
        Assert.Equal("https://example.com/long/path", result.DestinationUrl);
    }

    [Fact]
    public async Task CreateLinkAsync_WithExistingCustomCode_ThrowsConflict()
    {
        var repository = new StubLinkRepository
        {
            Links =
            {
                ["ABCD12"] = new ShortLink
                {
                    Id = Guid.NewGuid(),
                    Code = "ABCD12",
                    DestinationUrl = "https://example.com",
                    CreatedAtUtc = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc),
                    AccessCount = 0
                }
            }
        };

        var service = new LinkService(repository, new FakeClock(new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateLinkAsync(new CreateLinkRequest("https://example.com/other", "abcd12", null), "127.0.0.1"));
    }

    [Fact]
    public async Task RedirectLinkAsync_WhenActive_IncrementsAccessCount()
    {
        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = "ABC1234",
            DestinationUrl = "https://example.com/final",
            CreatedAtUtc = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc),
            AccessCount = 0
        };

        var repository = new StubLinkRepository
        {
            Links = { [link.Code] = link }
        };

        var service = new LinkService(repository, new FakeClock(new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc)));

        var result = await service.RedirectLinkAsync("abc1234");

        Assert.Equal("https://example.com/final", result.DestinationUrl);
        Assert.Equal(1, result.AccessCount);
    }

    private sealed class StubLinkRepository : ILinkRepository
    {
        public Dictionary<string, ShortLink> Links { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string SourceAddress, DateTime CreatedAtUtc)> Attempts { get; } = new();

        public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Links.TryGetValue(code, out var link) ? link : null);

        public Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Links.ContainsKey(code));

        public Task<int> CountRecentCreationAttemptsAsync(string sourceAddress, DateTime cutoffUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Attempts.Count(attempt => attempt.SourceAddress == sourceAddress && attempt.CreatedAtUtc >= cutoffUtc));

        public Task<ShortLink?> CreateAsync(ShortLink link, string sourceAddress, CancellationToken cancellationToken = default)
        {
            if (Links.ContainsKey(link.Code))
            {
                return Task.FromResult<ShortLink?>(null);
            }

            Links[link.Code] = link;
            Attempts.Add((sourceAddress, link.CreatedAtUtc));
            return Task.FromResult<ShortLink?>(link);
        }

        public Task<ShortLink?> IncrementAccessCountIfActiveAsync(string code, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            if (!Links.TryGetValue(code, out var link) || (link.ExpiresAtUtc.HasValue && link.ExpiresAtUtc.Value <= utcNow))
            {
                return Task.FromResult<ShortLink?>(null);
            }

            link.AccessCount += 1;
            return Task.FromResult<ShortLink?>(link);
        }

        public Task<ShortLink?> GetStatsAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Links.TryGetValue(code, out var link) ? link : null);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }
}
