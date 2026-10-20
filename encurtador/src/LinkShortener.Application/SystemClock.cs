using LinkShortener.Application.Abstractions;

namespace LinkShortener.Application;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
