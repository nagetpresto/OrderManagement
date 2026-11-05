using Adaya.Orders.Application.Orders;
using Adaya.Orders.Domain.Entities;

namespace Adaya.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct);
    Task<Order?> GetByIdempotencyKeyAsync(int customerId, string key, CancellationToken ct);
    Task<(IReadOnlyList<Order> Items, int Total)> ListAsync(ListOrdersQuery query, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}