using Adaya.Orders.Application.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Adaya.Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddScoped<IOrderService, OrderService>();
}