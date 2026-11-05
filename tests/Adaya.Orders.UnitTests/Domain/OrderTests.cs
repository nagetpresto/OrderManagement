using Adaya.Orders.Domain.Entities;
using Adaya.Orders.Domain.Enums;
using Adaya.Orders.Domain.Exceptions;

namespace Adaya.Orders.UnitTests.Domain;

public class OrderTests
{
    // trasisi status
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending]   = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped]   = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IEnumerable<object[]> AllTransitions()
    {
        foreach (var from in Enum.GetValues<OrderStatus>())
            foreach (var to in Enum.GetValues<OrderStatus>())
                yield return [from, to, Allowed[from].Contains(to)];
    }

    [Theory]
    [MemberData(nameof(AllTransitions))]
    public void ChangeStatus_FollowsTransitionTable(OrderStatus from, OrderStatus to, bool allowed)
    {
        var order = OrderIn(from);

        if (allowed)
        {
            order.ChangeStatus(to);
            Assert.Equal(to, order.Status);
        }
        else
        {
            Assert.Throws<InvalidStatusTransitionException>(() => order.ChangeStatus(to));
            Assert.Equal(from, order.Status);
        }
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = Order.Create(1, "Jl. Test", "key", [(1, 2, 1000m)]);
        foreach (var step in PathTo(status)) order.ChangeStatus(step);
        return order;
    }

    private static OrderStatus[] PathTo(OrderStatus status) => status switch
    {
        OrderStatus.Pending   => [],
        OrderStatus.Confirmed => [OrderStatus.Confirmed],
        OrderStatus.Shipped   => [OrderStatus.Confirmed, OrderStatus.Shipped],
        OrderStatus.Delivered => [OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered],
        OrderStatus.Cancelled => [OrderStatus.Cancelled],
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
