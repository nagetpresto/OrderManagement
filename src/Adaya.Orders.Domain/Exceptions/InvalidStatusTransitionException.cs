using Adaya.Orders.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Adaya.Orders.Domain.Exceptions
{
    public class InvalidStatusTransitionException(OrderStatus from, OrderStatus to)
    : DomainException($"Transisi status dari {from} ke {to} tidak valid.")
    {
        public OrderStatus From { get; } = from;
        public OrderStatus To { get; } = to;
    }
}
