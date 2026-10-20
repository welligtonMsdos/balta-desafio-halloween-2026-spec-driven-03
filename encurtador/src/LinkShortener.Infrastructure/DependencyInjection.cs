using LinkShortener.Application;
using LinkShortener.Application.Abstractions;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLinkShortenerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=/data/linkshortener.db";

        services.AddDbContext<LinkShortenerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
