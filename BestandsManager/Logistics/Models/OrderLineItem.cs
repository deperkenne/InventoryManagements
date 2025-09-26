namespace BestandsManager.Logistics.Model
{
    public class OrderLineItem
    {
        private string Id { get; set; } = Guid.NewGuid().ToString();
        private string ProductName { get; set; }
        private int QuantityRequested { get; set; }
        private string Skus { get; set; } 
        public OrderLineItem(string productName, int Quantity,string sku)
        {
            ProductName = productName;
            QuantityRequested = Quantity;
            Skus = sku;
        }

        public override string ToString()
        {
            string orderlinesku = "";
            foreach (var sku in Skus)
            {
                orderlinesku += sku + ";";
            }
            return $" linenumber: {Id} productname: {ProductName} quantityrequested: {QuantityRequested}\n orderLineSku:[ {orderlinesku} ]";
        }

        public int GetQuantityRequested()
        {
            return QuantityRequested;
        }

        public void SetQuantityRequested(int quantityRequested)
        {
            QuantityRequested = quantityRequested;
        }

        public string GetOrderId()
        {
            return Id;
        }

        public string GetProductName()
        {
            return ProductName;
        }

        public string GetSkus()
        {
            return Skus;
        }

        public void SetSkus(string skus)
        {
            this.Skus = skus;
        }

        public void SetProductName(string productName)
        {
            this.ProductName = productName;
        }

        public void SetOrderId(string orderId)
        {
            Id = orderId;
        }

    }
}
