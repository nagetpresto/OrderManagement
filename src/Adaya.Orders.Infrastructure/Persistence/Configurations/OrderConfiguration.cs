using Adaya.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adaya.Orders.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).UseIdentityAlwaysColumn();
        b.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(500);
        b.Property(o => o.IdempotencyKey).IsRequired().HasMaxLength(100);
        b.Property(o => o.TotalAmount).HasPrecision(18, 2);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.Version).IsRowVersion();
        b.HasIndex(o => new { o.CustomerId, o.IdempotencyKey }).IsUnique();
        b.HasIndex(o => o.Status);
        b.HasIndex(o => o.CustomerId);
        b.HasIndex(o => o.CreatedAt);

        b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}