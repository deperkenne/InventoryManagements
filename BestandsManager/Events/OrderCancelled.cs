using InventoryManagement.Logistics.Models;

namespace InventoryManagement.Events
{
    internal class OrderCancelled : DomainEvent
    {
        public string OrderId { get; }
        public OrderCancelled(  string orderId)
        {
            OrderId = orderId;
        }

    }
}
