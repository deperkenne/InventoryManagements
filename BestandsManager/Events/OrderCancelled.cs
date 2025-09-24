using BestandsManager.Logistics.Model;

namespace BestandsManager.Event
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
