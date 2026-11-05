using Adaya.Orders.Application.Abstractions;
using Adaya.Orders.Application.Common;
using Adaya.Orders.Domain.Entities;
using Adaya.Orders.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Adaya.Orders.Application.Orders;

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    IUnitOfWork uow,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<CreateOrderResult> CreateAsync(string idempotencyKey, CreateOrderRequest request, CancellationToken ct)
    {
        Validate(idempotencyKey, request);

        var lines = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .OrderBy(l => l.ProductId)
            .ToList();

        // scenario C => validasi duplikat idempotencykey
        var existing = await orders.GetByIdempotencyKeyAsync(request.CustomerId, idempotencyKey, ct);
        if (existing is not null)
            return Replay(existing, request, lines);

        try
        {
            var order = await uow.ExecuteInTransactionAsync(async token =>
            {
                var prices = await products.GetPricesAsync(lines.Select(l => l.ProductId), token);

                foreach (var line in lines)
                    if (!prices.ContainsKey(line.ProductId))
                        throw new NotFoundException("Product", line.ProductId);

                foreach (var line in lines)
                {
                    if (!await products.TryDeductStockAsync(line.ProductId, line.Quantity, token))
                        throw new InsufficientStockException(line.ProductId, line.Quantity);
                }

                var newOrder = Order.Create(
                    request.CustomerId, request.ShippingAddress, idempotencyKey,
                    lines.Select(l => (l.ProductId, l.Quantity, prices[l.ProductId])));

                await orders.AddAsync(newOrder, token);
                await uow.SaveChangesAsync(token);
                return newOrder;
            }, ct);

            logger.LogInformation("Order {OrderId} dibuat untuk customer {CustomerId}", order.Id, order.CustomerId);
            return new CreateOrderResult(OrderDto.From(order), IsReplay: false);
        }
        catch (DuplicateIdempotencyKeyException)
        {
            logger.LogWarning("Race idempotency terdeteksi untuk key {Key}; mengembalikan order pemenang", idempotencyKey);
            var winner = await orders.GetByIdempotencyKeyAsync(request.CustomerId, idempotencyKey, ct)
                         ?? throw new InvalidOperationException("Order pemenang tidak ditemukan.");
            return Replay(winner, request, lines);
        }
        catch (InsufficientStockException)
        {
            // Request kembar kalah balapan DAN stok sudah dihabiskan pemenang.
            // Kalau key ini sudah punya order, ini replay, bukan "stok kurang".
            var winner = await orders.GetByIdempotencyKeyAsync(request.CustomerId, idempotencyKey, ct);
            if (winner is null) throw; // stok memang kurang, tidak terkait duplikat

            logger.LogWarning("Request kembar dengan key {Key} gagal di stok; mengembalikan order pemenang", idempotencyKey);
            return Replay(winner, request, lines);
        }
    }

    public async Task<OrderDto> GetAsync(int id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct) ?? throw new NotFoundException("Order", id);
        return OrderDto.From(order);
    }

    public async Task<PagedResult<OrderDto>> ListAsync(ListOrdersQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = query with { Page = page, PageSize = pageSize };

        var (items, total) = await orders.ListAsync(normalized, ct);
        return new PagedResult<OrderDto>(items.Select(OrderDto.From).ToList(), page, pageSize, total);
    }

    public Task<OrderDto> UpdateStatusAsync(int id, OrderStatus newStatus, CancellationToken ct)
        => ChangeStatusAsync(id, newStatus, ct);

    public Task<OrderDto> CancelAsync(int id, CancellationToken ct)
        => ChangeStatusAsync(id, OrderStatus.Cancelled, ct);

    private Task<OrderDto> ChangeStatusAsync(int id, OrderStatus newStatus, CancellationToken ct)
        => uow.ExecuteInTransactionAsync(async token =>
        {
            var order = await orders.GetByIdAsync(id, token) ?? throw new NotFoundException("Order", id);
            var oldStatus = order.Status;

            order.ChangeStatus(newStatus); 
            
            await uow.SaveChangesAsync(token);

            if (newStatus == OrderStatus.Cancelled)
            {
                foreach (var item in order.Items.OrderBy(i => i.ProductId))
                    await products.RestoreStockAsync(item.ProductId, item.Quantity, token);
            }

            logger.LogInformation("Order {OrderId} status {From} -> {To}", id, oldStatus, newStatus);
            return OrderDto.From(order);
        }, ct);

    private static CreateOrderResult Replay(Order existing, CreateOrderRequest request,
        List<(int ProductId, int Quantity)> lines)
    {
        var existingLines = existing.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .OrderBy(l => l.ProductId);

        var same = existing.ShippingAddress == request.ShippingAddress && existingLines.SequenceEqual(lines);
        if (!same) throw new IdempotencyKeyReuseException();

        return new CreateOrderResult(OrderDto.From(existing), IsReplay: true);
    }

    private static void Validate(string idempotencyKey, CreateOrderRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            errors["Idempotency-Key"] = ["Header Idempotency-Key wajib diisi (maks 100 karakter)."];
        if (r.CustomerId <= 0)
            errors["customerId"] = ["customerId harus lebih dari 0."];
        if (string.IsNullOrWhiteSpace(r.ShippingAddress))
            errors["shippingAddress"] = ["shippingAddress wajib diisi."];
        if (r.Items is null || r.Items.Count == 0)
            errors["items"] = ["Minimal 1 item."];
        else if (r.Items.Any(i => i.Quantity <= 0 || i.ProductId <= 0))
            errors["items"] = ["productId dan quantity harus lebih dari 0."];

        if (errors.Count > 0) throw new RequestValidationException(errors);
    }
}