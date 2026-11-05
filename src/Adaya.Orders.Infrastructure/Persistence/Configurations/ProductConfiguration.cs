using Adaya.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adaya.Orders.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", t =>
            t.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "\"StockQuantity\" >= 0"));

        b.HasKey(p => p.Id);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.Price).HasPrecision(18, 2);

        // seed dataa
        b.HasData(
            new { Id = 1, Name = "Product X", StockQuantity = 15, Price = 100000m },
            new { Id = 2, Name = "Product Y", StockQuantity = 100, Price = 25000m },
            new { Id = 3, Name = "Product Z", StockQuantity = 0, Price = 50000m });
    }
}