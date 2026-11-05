using Adaya.Orders.Domain.Exceptions;

namespace Adaya.Orders.Domain.Entities;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public decimal Price { get; private set; }

    private Product() { }

    public Product(int id, string name, int stockQuantity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nama produk wajib diisi.");
        if (stockQuantity < 0) throw new DomainException("Stock tidak boleh negatif.");
        if (price < 0) throw new DomainException("Harga tidak boleh negatif.");
        Id = id; Name = name; StockQuantity = stockQuantity; Price = price;
    }
}