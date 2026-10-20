namespace LinkShortener.Domain;

public class ShortLink
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public long AccessCount { get; set; }

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value;
}
