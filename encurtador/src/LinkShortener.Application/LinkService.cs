using LinkShortener.Application.Abstractions;
using LinkShortener.Domain;

namespace LinkShortener.Application;

public sealed record CreateLinkRequest(string? DestinationUrl, string? CustomCode, DateTimeOffset? ExpiresAt);
public sealed record CreateLinkResult(string Code, string ShortUrl, string DestinationUrl, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt);
public sealed record RedirectResult(string DestinationUrl, long AccessCount);
public sealed record LinkStatsResult(string Code, long AccessCount, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt);

public sealed class LinkService
{
    private const int MaxRecentSuccessfulCreatiions = 10;
    private const int CreationWindowSeconds = 60;
    private readonly ILinkRepository _repository;
    private readonly IClock _clock;

    public LinkService(ILinkRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, string sourceAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceAddress))
        {
            throw new ValidationException("O endereço de origem é obrigatório.");
        }

        var destination = request.DestinationUrl?.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ValidationException("A URL de destino é obrigatória.");
        }

        if (!Uri.TryCreate(destination, UriKind.Absolute, out var uri) ||
            (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
             !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException("A URL de destino deve usar HTTP ou HTTPS.");
        }

        if (uri.OriginalString.Length > 2048)
        {
            throw new ValidationException("A URL de destino deve ter no máximo 2048 caracteres.");
        }

        var customCode = string.IsNullOrWhiteSpace(request.CustomCode) ? null : LinkCodeGenerator.NormalizeCode(request.CustomCode);
        var code = customCode ?? LinkCodeGenerator.GenerateUniqueCode();

        var now = _clock.UtcNow;
        DateTime? expiresAtUtc = request.ExpiresAt is DateTimeOffset offset ? offset.UtcDateTime : null;
        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= now)
        {
            throw new ValidationException("A data de expiração deve ser no futuro.");
        }

        var canonicalCode = LinkCodeGenerator.NormalizeCode(code);
        var cutoff = now.AddSeconds(-CreationWindowSeconds);
        var attemptsInWindow = await _repository.CountRecentCreationAttemptsAsync(sourceAddress.Trim(), cutoff, cancellationToken);
        if (attemptsInWindow >= MaxRecentSuccessfulCreatiions)
        {
            throw new RateLimitExceededException("Você atingiu o limite de 10 criações nos últimos 60 segundos.");
        }

        if (customCode is not null && await _repository.IsCodeTakenAsync(canonicalCode, cancellationToken))
        {
            throw new ConflictException("Este código curto já está em uso.");
        }

        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = canonicalCode,
            DestinationUrl = uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            AccessCount = 0
        };

        var created = await _repository.CreateAsync(link, sourceAddress.Trim(), cancellationToken);
        if (created is null)
        {
            throw new ConflictException("Este código curto já está em uso.");
        }

        return new CreateLinkResult(
            created.Code,
            $"http://localhost:8080/{created.Code}",
            created.DestinationUrl,
            new DateTimeOffset(created.CreatedAtUtc),
            created.ExpiresAtUtc is DateTime expires ? new DateTimeOffset(expires) : null);
    }

    public async Task<RedirectResult> RedirectLinkAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = LinkCodeGenerator.NormalizeCode(code);
        var now = _clock.UtcNow;

        var existing = await _repository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Código não encontrado.");
        }

        if (existing.IsExpired(now))
        {
            throw new ExpiredLinkException("Este link expirou.");
        }

        var updated = await _repository.IncrementAccessCountIfActiveAsync(normalizedCode, now, cancellationToken);
        if (updated is null)
        {
            var reloaded = await _repository.GetByCodeAsync(normalizedCode, cancellationToken);
            if (reloaded is null)
            {
                throw new NotFoundException("Código não encontrado.");
            }

            if (reloaded.IsExpired(now))
            {
                throw new ExpiredLinkException("Este link expirou.");
            }

            throw new ValidationException("Não foi possível processar o redirecionamento.");
        }

        return new RedirectResult(updated.DestinationUrl, updated.AccessCount);
    }

    public async Task<LinkStatsResult> GetLinkStatsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = LinkCodeGenerator.NormalizeCode(code);
        var result = await _repository.GetStatsAsync(normalizedCode, cancellationToken);
        if (result is null)
        {
            throw new NotFoundException("Código não encontrado.");
        }

        return new LinkStatsResult(
            result.Code,
            result.AccessCount,
            new DateTimeOffset(result.CreatedAtUtc),
            result.ExpiresAtUtc is DateTime expires ? new DateTimeOffset(expires) : null);
    }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message)
    {
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ExpiredLinkException : Exception
{
    public ExpiredLinkException(string message) : base(message)
    {
    }
}
