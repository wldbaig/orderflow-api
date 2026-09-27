using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(30);
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CustomerReference).HasMaxLength(100);
        builder.Property(o => o.SpecialInstructions).HasMaxLength(4000);

        // Enums stored as int to preserve ordinal ordering (Low < Medium < High) for sorting.
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.Priority).HasConversion<int>();
        builder.Property(o => o.AiAnalysisStatus).HasConversion<int>();

        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.AiSummary).HasMaxLength(1000);
        builder.Property(o => o.AiRiskFlags).HasMaxLength(2000);

        // Lines are part of the aggregate; access through the backing field only.
        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // --- Indexes for the main filter/sort columns (see PERFORMANCE.md) ---
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.CreatedAtUtc);                              // default sort
        builder.HasIndex(o => new { o.Status, o.CreatedAtUtc });           // filter by status + sort
        builder.HasIndex(o => new { o.Priority, o.CreatedAtUtc });         // filter by priority + sort
        builder.HasIndex(o => o.TotalUnits);
        builder.HasIndex(o => o.TotalAmount);
    }
}
