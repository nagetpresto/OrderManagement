using System.Net;
using System.Net.Http.Json;
using System.Text;
using Adaya.Orders.Api.Models;
using Adaya.Orders.Application.Orders;
using Adaya.Orders.Domain.Entities;
using Adaya.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adaya.Orders.IntegrationTests;

[Collection("api")]
public abstract class IntegrationTestBase(ApiFactory factory)
{
    private static int _nextId = 1000;
    protected static int NextId() => Interlocked.Increment(ref _nextId);

    protected readonly HttpClient Client = factory.CreateClient();

    protected async Task<int> SeedProductAsync(int stock)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = NextId();
        db.Products.Add(new Product(id, $"Test-{id}", stock, 10_000m));
        await db.SaveChangesAsync();
        return id;
    }

    protected async Task<int> GetStockAsync(int productId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.StockQuantity)
            .SingleAsync();
    }

    protected async Task<int> CountOrdersAsync(int customerId, string key)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.CountAsync(o => o.CustomerId == customerId && o.IdempotencyKey == key);
    }

    protected Task<HttpResponseMessage> PostOrderAsync(string key, int customerId, int productId, int quantity)
    {
        var body = new
        {
            customerId,
            shippingAddress = "Jl. Test No. 1, Bandung",
            items = new[] { new { productId, quantity } }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        return Client.SendAsync(request);
    }

    protected Task<HttpResponseMessage> PatchStatusAsync(int orderId, string status)
        => Client.PatchAsync($"/api/orders/{orderId}/status",
            new StringContent($"{{\"status\":\"{status}\"}}", Encoding.UTF8, "application/json"));

    protected Task<HttpResponseMessage> CancelAsync(int orderId)
        => Client.PostAsync($"/api/orders/{orderId}/cancel", null);

    protected async Task<OrderDto> CreateOrderAsync(int productId, int quantity)
    {
        var response = await PostOrderAsync($"k-{NextId()}", NextId(), productId, quantity);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    protected async Task<OrderDto> GetOrderAsync(int orderId)
        => (await Client.GetFromJsonAsync<OrderDto>($"/api/orders/{orderId}"))!;

    protected static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<ApiError>())?.Code;

    protected static async Task<HttpResponseMessage[]> RunConcurrentlyAsync(
        int count, Func<int, Task<HttpResponseMessage>> action)
    {
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Run(async () =>
            {
                await gate.Task;
                return await action(i);
            }))
            .ToList();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }
}
