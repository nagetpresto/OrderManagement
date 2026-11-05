using System.Net;
using System.Net.Http.Json;
using Adaya.Orders.Application.Orders;

namespace Adaya.Orders.IntegrationTests;

public class ConcurrencyScenarioTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    // ---------- Skenario A: pengurangan stok bersamaan ----------

    [Fact]
    public async Task ScenarioA_TwoOrdersOf10_WhenStockIs15_OnlyOneSucceeds()
    {
        var productId = await SeedProductAsync(stock: 15);

        var responses = await RunConcurrentlyAsync(2,
            _ => PostOrderAsync($"a-{NextId()}", NextId(), productId, quantity: 10));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal("insufficient_stock", await ErrorCodeAsync(rejected));
        Assert.Equal(5, await GetStockAsync(productId));
    }

    // ---------- Skenario B: dua admin mengubah status bersamaan ----------

    [Fact]
    public async Task ScenarioB_ShipAndCancelAtTheSameTime_OnlyOneWins_StockStaysConsistent()
    {
        for (var round = 0; round < 5; round++) 
        {
            var productId = await SeedProductAsync(stock: 100);
            var order = await CreateOrderAsync(productId, quantity: 1);
            Assert.Equal(HttpStatusCode.OK, (await PatchStatusAsync(order.Id, "Confirmed")).StatusCode);

            var responses = await RunConcurrentlyAsync(2, i =>
                i == 0 ? PatchStatusAsync(order.Id, "Shipped") : CancelAsync(order.Id));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            var loser = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Contains(await ErrorCodeAsync(loser),
                new[] { "concurrency_conflict", "invalid_status_transition" });

            var final = await GetOrderAsync(order.Id);
            var expectedStock = final.Status == "Cancelled" ? 100 : 99;
            Assert.Equal(expectedStock, await GetStockAsync(productId));
        }
    }

    // ---------- Skenario C: request kembar dengan Idempotency-Key sama ----------

    [Theory]
    [InlineData(10)] 
    [InlineData(100)]
    public async Task ScenarioC_SameKeyAtTheSameTime_CreatesOnlyOneOrder(int initialStock)
    {
        var productId = await SeedProductAsync(initialStock);
        var customerId = NextId();
        var key = $"c-{NextId()}";

        var responses = await RunConcurrentlyAsync(5,
            _ => PostOrderAsync(key, customerId, productId, quantity: 10));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.OK));

        var orderIds = new HashSet<int>();
        foreach (var response in responses)
            orderIds.Add((await response.Content.ReadFromJsonAsync<OrderDto>())!.Id);
        Assert.Single(orderIds);

        Assert.Equal(1, await CountOrdersAsync(customerId, key));
        Assert.Equal(initialStock - 10, await GetStockAsync(productId));
    }

    // ---------- Race tambahan: cancel ganda ----------

    [Fact]
    public async Task DoubleCancel_AtTheSameTime_RestoresStockOnlyOnce()
    {
        for (var round = 0; round < 5; round++)
        {
            var productId = await SeedProductAsync(stock: 50);
            var order = await CreateOrderAsync(productId, quantity: 5);

            var responses = await RunConcurrentlyAsync(2, _ => CancelAsync(order.Id));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(50, await GetStockAsync(productId));
        }
    }
}
