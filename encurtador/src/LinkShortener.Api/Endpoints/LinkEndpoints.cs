using LinkShortener.Application;
using Microsoft.AspNetCore.Http;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static void MapLinkEndpoints(this WebApplication app)
    {
        app.MapPost("/api/links", async (CreateLinkRequest request, HttpContext httpContext, LinkService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var ip = GetSourceAddress(httpContext);
                var result = await service.CreateLinkAsync(request, ip, cancellationToken);

                return Results.Created(
                    result.ShortUrl,
                    new
                    {
                        code = result.Code,
                        shortUrl = result.ShortUrl,
                        destinationUrl = result.DestinationUrl,
                        createdAt = result.CreatedAt,
                        expiresAt = result.ExpiresAt
                    });
            }
            catch (ValidationException ex)
            {
                return Results.Problem(title: "Dados inválidos", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (ConflictException ex)
            {
                return Results.Problem(title: "Código em uso", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (RateLimitExceededException ex)
            {
                return Results.Problem(title: "Limite atingido", detail: ex.Message, statusCode: StatusCodes.Status429TooManyRequests);
            }
        });

        app.MapGet("/api/links/{code}/stats", async (string code, LinkService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetLinkStatsAsync(code, cancellationToken);
                return Results.Ok(new
                {
                    code = result.Code,
                    accessCount = result.AccessCount,
                    createdAt = result.CreatedAt,
                    expiresAt = result.ExpiresAt
                });
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(title: "Código não encontrado", detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        app.MapGet("/{code}", async (string code, LinkService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.RedirectLinkAsync(code, cancellationToken);
                return Results.Redirect(result.DestinationUrl, false);
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(title: "Código não encontrado", detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ExpiredLinkException ex)
            {
                return Results.Problem(title: "Link expirado", detail: ex.Message, statusCode: StatusCodes.Status410Gone);
            }
        });
    }

    private static string GetSourceAddress(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
