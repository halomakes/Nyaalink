using Microsoft.EntityFrameworkCore;

namespace Nyaalink;

public class DownloadRecord
{
    public ulong Id { get; set; }
    public required string Title { get; set; }
    public DateTime IngestedAt { get; set; }
    public DateTime? DownloadedAt { get; set; }
    public Uri MagnetLink { get; set; }
    public uint RuleId { get; set; }
    public virtual DownloadRule? Rule { get; set; }
}

public class DownloadRule
{
    public uint Id { get; set; }
    public required string Label { get; set; }
    public required string Pattern { get; set; }
    public ulong AniDbId { get; set; }

    public virtual ICollection<DownloadQuery>? Queries { get; set; }

    public override string ToString() => $"[{Id}] {Label}";

    public bool Matches(Item feedItem) =>
        feedItem.Title?.Contains(Pattern, StringComparison.InvariantCultureIgnoreCase) ?? false;
}

public class DownloadQuery
{
    public uint Id { get; set; }
    public required string Query { get; set; }
    public DateTime? LastFetched { get; set; }
    
    public virtual ICollection<DownloadRule> Rules { get; set; }
}

internal class DownloadContext(DbContextOptions<DownloadContext> options) : DbContext(options)
{
    public virtual DbSet<DownloadRecord> Records { get; set; } = null!;
    public virtual DbSet<DownloadQuery> Queries { get; set; } = null!;
    public virtual DbSet<DownloadRule> Rules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadRule>(static rule =>
        {
            rule.HasKey(static r => r.Id);
            rule.Property(static r => r.Id)
                .ValueGeneratedOnAdd();
            rule.HasMany<DownloadQuery>(static r => r.Queries)
                .WithMany(static r => r.Rules);
            rule.HasMany<DownloadRecord>()
                .WithOne(static r => r.Rule)
                .HasForeignKey(static r => r.RuleId)
                .HasPrincipalKey(static r => r.Id);
        });

        modelBuilder.Entity<DownloadQuery>(static query =>
        {
            query.HasKey(static q => q.Id);
            query.Property(static q => q.Id)
                .ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<DownloadRecord>(static record =>
        {
            record.HasKey(static r => r.Id);
            record.Property(static r => r.Title)
                .IsRequired()
                .HasMaxLength(500);
        });
    }
}