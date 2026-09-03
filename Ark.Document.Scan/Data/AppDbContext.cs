using System.Text.Json;
using Ark.Document.Scan.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ark.Document.Scan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var scanJob = modelBuilder.Entity<ScanJob>();

        scanJob.Property(j => j.Corners)
            .HasConversion(
                corners => JsonSerializer.Serialize(corners, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<CornerPoint[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<CornerPoint>(),
                new ValueComparer<CornerPoint[]>(
                    (a, b) => (a ?? Array.Empty<CornerPoint>()).SequenceEqual(b ?? Array.Empty<CornerPoint>()),
                    arr => arr.Aggregate(0, (hash, p) => HashCode.Combine(hash, p.X, p.Y)),
                    arr => arr.ToArray()));

        scanJob.Property(j => j.FilterMode)
            .HasConversion<string>();

        scanJob.HasIndex(j => j.ShareToken).IsUnique();
    }
}
