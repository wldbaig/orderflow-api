using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductSku).IsRequired().HasMaxLength(50);
        builder.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);

        // LineTotal is derived in the domain — not a stored column.
        builder.Ignore(l => l.LineTotal);

        builder.HasIndex(l => l.OrderId);
    }
}
