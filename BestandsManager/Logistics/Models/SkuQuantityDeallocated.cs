namespace BestandsManager.Logistics.Model
{
    public class SkuQuantityDeallocated : DomainEvent
    {

        public string SkuId { get; }
        public int QuantityDeallocated { get; }
        public string OrderId { get; }
        public EnumOrderStatus orderStatus { get; }
        public string LineNumber { get; }
        public string EventType { get; }

        public SkuQuantityDeallocated(string skuId, int quantity, string orderId, string lineNumber = "", string eventType = "deallocation")
        {
            this.SkuId = skuId;
            this.LineNumber = lineNumber;
            this.QuantityDeallocated = quantity;
            this.OrderId = orderId;
            this.EventType = eventType;
        }
    }
}
