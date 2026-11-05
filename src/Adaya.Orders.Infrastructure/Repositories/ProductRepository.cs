using Adaya.Orders.Application.Abstractions;
using Adaya.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adaya.Orders.Infrastructure.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<Dictionary<int, decimal>> GetPricesAsync(IEnumerable<int> productIds, CancellationToken ct)
    {
        var ids = productIds.ToList();
        return await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Price, ct);
    }

    public async Task<bool> TryDeductStockAsync(int productId, int quantity, CancellationToken ct)
    {
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Products""
            SET ""StockQuantity"" = ""StockQuantity"" - {quantity}
            WHERE ""Id"" = {productId} AND ""StockQuantity"" >= {quantity}", ct);

        return affected == 1;
    }

    public async Task RestoreStockAsync(int productId, int quantity, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Products""
            SET ""StockQuantity"" = ""StockQuantity"" + {quantity}
            WHERE ""Id"" = {productId}", ct);
    }
}