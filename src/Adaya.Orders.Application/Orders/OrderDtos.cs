using Adaya.Orders.Domain.Entities;
using Adaya.Orders.Domain.Enums;

namespace Adaya.Orders.Application.Orders;

public record OrderItemRequest(int ProductId, int Quantity);
public record CreateOrderRequest(int CustomerId, string ShippingAddress, List<OrderItemRequest> Items);
public record UpdateOrderStatusRequest(OrderStatus Status);

public record ListOrdersQuery(
    OrderStatus? Status = null, int? CustomerId = null,
    DateTime? From = null, DateTime? To = null,
    int Page = 1, int PageSize = 20);

public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);

public record OrderDto(int Id, int CustomerId, string ShippingAddress, string Status,
    decimal TotalAmount, DateTime CreatedAt, DateTime UpdatedAt, List<OrderItemDto> Items)
{
    public static OrderDto From(Order o) => new(
        o.Id, o.CustomerId, o.ShippingAddress, o.Status.ToString(), o.TotalAmount,
        o.CreatedAt, o.UpdatedAt,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.UnitPrice)).ToList());
}

public record CreateOrderResult(OrderDto Order, bool IsReplay);