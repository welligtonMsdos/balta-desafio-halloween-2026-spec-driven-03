namespace LinkShortener.Domain;

public class LinkCreationAttempt
{
    public Guid Id { get; set; }
    public string SourceAddress { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
