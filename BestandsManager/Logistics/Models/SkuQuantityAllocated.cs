namespace BestandsManager.Logistics.Model
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
            this.SkuId = skuId;
            this.QuantityAllocated = quantity;
            this.OrderId = orderId;
            this.EventType = eventType;
            this.LineNumber = lineNumber;
            this.Status = enumStatus;
        }
    }
}
