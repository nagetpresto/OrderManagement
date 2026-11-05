namespace Adaya.Orders.Domain.Entities;

public class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderItem() { }

    internal OrderItem(int productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}