using ExpenseAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseAssistant.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<ExpenseDocument> ExpenseDocuments => Set<ExpenseDocument>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExpenseDocument>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.Category)
                .HasMaxLength(100);

            entity.Property(x => x.Currency)
                .HasMaxLength(10);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.IsProcessed)
                .IsRequired();
        });
    }
}