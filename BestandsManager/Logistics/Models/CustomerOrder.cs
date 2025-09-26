namespace BestandsManager.Logistics.Model
{
    public class CustomerOrder
    {

        private String Id { get; set; }
        private List<OrderLineItem> OrderLines { get; set; } = new List<OrderLineItem>();
        public DateTime OrderDate { get; }
        public bool IsCompleteDeliveryRequired { get; }
        public EnumOrderPriority Priority { get; }

        private EnumOrderStatus Status;

        public CustomerOrder(string Id,DateTime OrderDate, bool iscompleteDeliveryRequired = false, EnumOrderPriority priority = EnumOrderPriority.Normal, EnumOrderStatus enumStatus = EnumOrderStatus.NEW)
        {
            this.Id = Id;
            this.OrderDate = OrderDate;
            this.IsCompleteDeliveryRequired = iscompleteDeliveryRequired;
            this.Priority = priority;
            this.Status = enumStatus;   
        }

        public CustomerOrder(List<OrderLineItem> orderLines)
        {
            this.OrderLines = orderLines;
        }

        public string GetId()
        {
            return Id;
        }

        public List<OrderLineItem> GetOrderLineItems()
        {
            return OrderLines;
        }

        public EnumOrderStatus GetOrderStatus()
        {
            return Status;
        }

        public void SetEnumStatus(EnumOrderStatus status)
        {
            this.Status = status;
        }

        public void SetOrderLineItems(List<OrderLineItem> orderLineItems)
        {
            this.OrderLines = orderLineItems;
        }

        public List<OrderLineItem> GetOrderLines()
        {
            return OrderLines;
        }

        public void AddOrderLine(OrderLineItem orderLineItem)
        {
            this.OrderLines.Add(orderLineItem);
        }

        public void printOrderDetail()
        {
            string orderLine = "";
            foreach (var line in OrderLines)
            {

                orderLine += line.ToString();

            }
            string OrderInfo = $"orderId: {Id} orderdate: {OrderDate} completedeliveryrequired: {IsCompleteDeliveryRequired} priority: {Priority} status: {Status} \n orderLine:[{orderLine}]";
            Console.WriteLine(OrderInfo);
        }

    }
}
