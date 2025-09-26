namespace InventoryManagement.Logistics.Models
{
    public class SkuQuantityAllocated : DomainEvent
    {
        public string SkuId { get; }
        public int QuantityAllocated { get; }
        public string OrderId { get; }
        public EnumOrderStatus Status { get; }
        public string LineNumber { get; }
        public string EventType { get; }

        public SkuQuantityAllocated(string skuId, int quantity, string orderId, string eventType, string lineNumber, EnumOrderStatus enumStatus = EnumOrderStatus.RELEASED)
        {
            SkuId = skuId;
            QuantityAllocated = quantity;
            OrderId = orderId;
            EventType = eventType;
            LineNumber = lineNumber;
            Status = enumStatus;
        }
    }
}
