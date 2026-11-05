using Adaya.Orders.Application.Common;
using Adaya.Orders.Domain.Enums;

namespace Adaya.Orders.Application.Orders;

public interface IOrderService
{
    Task<CreateOrderResult> CreateAsync(string idempotencyKey, CreateOrderRequest request, CancellationToken ct);
    Task<OrderDto> GetAsync(int id, CancellationToken ct);
    Task<PagedResult<OrderDto>> ListAsync(ListOrdersQuery query, CancellationToken ct);
    Task<OrderDto> UpdateStatusAsync(int id, OrderStatus newStatus, CancellationToken ct);
    Task<OrderDto> CancelAsync(int id, CancellationToken ct);
}