using LinkShortener.Domain;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Infrastructure.Persistence;

public class LinkShortenerDbContext : DbContext
{
    public LinkShortenerDbContext(DbContextOptions<LinkShortenerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();
    public DbSet<LinkCreationAttempt> LinkCreationAttempts => Set<LinkCreationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLink>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.DestinationUrl).IsRequired().HasMaxLength(2048);
            entity.Property(x => x.CreatedAtUtc).HasColumnType("TEXT");
            entity.Property(x => x.ExpiresAtUtc).HasColumnType("TEXT");
            entity.Property(x => x.AccessCount).HasColumnType("INTEGER");
        });

        modelBuilder.Entity<LinkCreationAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceAddress).IsRequired().HasMaxLength(255);
            entity.Property(x => x.CreatedAtUtc).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.SourceAddress, x.CreatedAtUtc });
        });

        base.OnModelCreating(modelBuilder);
    }
}
