using Adaya.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adaya.Orders.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems", t =>
            t.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "\"Quantity\" > 0"));

        b.HasKey(i => i.Id);
        b.Property(i => i.Id).UseIdentityAlwaysColumn();
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);

        b.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}