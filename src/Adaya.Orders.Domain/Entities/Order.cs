using Adaya.Orders.Domain.Enums;
using Adaya.Orders.Domain.Exceptions;

namespace Adaya.Orders.Domain.Entities;

public class Order
{
    // transisi yg valid!
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    private readonly List<OrderItem> _items = [];

    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public string ShippingAddress { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint Version { get; private set; } // untuk versioning dan concurrency case
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(int customerId, string shippingAddress, string idempotencyKey,
    IEnumerable<(int ProductId, int Quantity, decimal UnitPrice)> items)
    {
        if (customerId <= 0) throw new DomainException("CustomerId wajib diisi.");
        if (string.IsNullOrWhiteSpace(shippingAddress)) throw new DomainException("ShippingAddress wajib diisi.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new DomainException("IdempotencyKey wajib diisi.");

        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            IdempotencyKey = idempotencyKey,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var (productId, quantity, unitPrice) in items)
        {
            if (quantity <= 0) throw new DomainException("Quantity harus lebih dari 0.");
            order._items.Add(new OrderItem(productId, quantity, unitPrice));
        }

        if (order._items.Count == 0) throw new DomainException("Order harus punya minimal 1 item.");

        order.TotalAmount = order._items.Sum(i => i.Quantity * i.UnitPrice);
        return order;
    }
    public void ChangeStatus(OrderStatus newStatus)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new InvalidStatusTransitionException(Status, newStatus);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel() => ChangeStatus(OrderStatus.Cancelled);
}