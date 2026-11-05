namespace Adaya.Orders.Application.Abstractions;

public interface IProductRepository
{
    Task<Dictionary<int, decimal>> GetPricesAsync(IEnumerable<int> productIds, CancellationToken ct);

    Task<bool> TryDeductStockAsync(int productId, int quantity, CancellationToken ct);

    Task RestoreStockAsync(int productId, int quantity, CancellationToken ct);
}