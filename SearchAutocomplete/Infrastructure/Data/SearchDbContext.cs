using Microsoft.EntityFrameworkCore;
using SearchAutocomplete.Domain.Entities;

namespace SearchAutocomplete.Infrastructure.Data;

public class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options)
    {
    }

    public DbSet<KinhThanh> KinhThanhs { get; set; }
    public DbSet<Section> Sections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure KinhThanh entity
        modelBuilder.Entity<KinhThanh>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.From).HasMaxLength(255);
            entity.Property(e => e.To).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Author).HasMaxLength(255);

            // Configure relationship
            entity.HasOne(e => e.Section)
                  .WithMany(s => s.KinhThanhs)
                  .HasForeignKey(e => e.SectionId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for search performance
            entity.HasIndex(e => e.Content).HasDatabaseName("IX_KinhThanhs_Content");
            entity.HasIndex(e => e.Type).HasDatabaseName("IX_KinhThanhs_Type");
            entity.HasIndex(e => e.Author).HasDatabaseName("IX_KinhThanhs_Author");
            entity.HasIndex(e => e.SectionId).HasDatabaseName("IX_KinhThanhs_SectionId");
        });

        // Configure Section entity
        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}