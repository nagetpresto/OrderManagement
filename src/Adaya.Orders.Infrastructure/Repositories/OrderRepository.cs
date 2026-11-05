using Adaya.Orders.Application.Abstractions;
using Adaya.Orders.Application.Orders;
using Adaya.Orders.Domain.Entities;
using Adaya.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adaya.Orders.Infrastructure.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(int id, CancellationToken ct)
        => db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdempotencyKeyAsync(int customerId, string key, CancellationToken ct)
        => db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.IdempotencyKey == key, ct);

    public async Task<(IReadOnlyList<Order> Items, int Total)> ListAsync(ListOrdersQuery q, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (q.Status is not null) query = query.Where(o => o.Status == q.Status);
        if (q.CustomerId is not null) query = query.Where(o => o.CustomerId == q.CustomerId);
        if (q.From is not null) query = query.Where(o => o.CreatedAt >= ToUtc(q.From.Value));
        if (q.To is not null) query = query.Where(o => o.CreatedAt <= ToUtc(q.To.Value));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Order order, CancellationToken ct)
        => await db.Orders.AddAsync(order, ct);

    private static DateTime ToUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };
}