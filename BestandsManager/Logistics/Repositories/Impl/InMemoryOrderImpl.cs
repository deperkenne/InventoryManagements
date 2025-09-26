using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Repositories.Impl
{
    // USE THIS CLASS ONLY FOR INTEGRATION TESTING!
    public class InMemoryOrderImpl : IOrderRepository
    {
        public List<CustomerOrder> orderMemory;
        public InMemoryOrderImpl()
        {
            InitializeOrders();
        }

        // Initialize with some orders
        public void InitializeOrders()
        {
            orderMemory = new List<CustomerOrder>(); 

            CustomerOrder order001 = new CustomerOrder("ORDER_001",new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High);
            order001.AddOrderLine(CreateOrderLine("ORANGE_L1", 30, "SKU_001"));
            order001.AddOrderLine(CreateOrderLine("WATER_L1", 50, "SKU_001"));
            orderMemory.Add(order001);

            CustomerOrder order002 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), true, EnumOrderPriority.High);
            order002.AddOrderLine(CreateOrderLine("ORANGE_L1", 50,  "SKU_002" ));
            order002.AddOrderLine(CreateOrderLine("WATER_L1", 30, "SKU_003" ));
            orderMemory.Add(order002);
            
            CustomerOrder order003 = new CustomerOrder("ORDER_003",new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.Normal);
            order003.AddOrderLine(CreateOrderLine("ORANGE_L1", 30, "SKU_002"));
            order003.AddOrderLine(CreateOrderLine("WATER_L1", 50, "SKU_002"));
            orderMemory.Add(order003);
        }

        // Helper method to create order lines
        private OrderLineItem CreateOrderLine(string ProductName, int Quantity,string sku)
        {
            return new OrderLineItem(ProductName, Quantity, sku);
        }

        public Task<CustomerOrder> SaveAsync(CustomerOrder order)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateStatusAsync(CustomerOrder customerOrder)
        {
            if (customerOrder == null)
            {
                throw new ArgumentNullException(nameof(customerOrder), "order muss not be null.");
            }

            var orderToUpdate = await GetByIdAsync(customerOrder.GetId());

            if (orderToUpdate != null)
            {
                orderToUpdate.SetEnumStatus(EnumOrderStatus.RELEASED);
            }
        }

        public async Task<List<CustomerOrder>> GetAllAsListAsync()
        {
            Task.Delay(1000).Wait();

            return orderMemory;
        }

        public async Task<CustomerOrder?> GetByIdAsync(string id)
        {
            if (id == string.Empty)
            {
                throw new ArgumentException("ID must not be empty.", nameof(id));
            }

            return orderMemory.Where(o => o.GetId() == id).First();
        }

        public async Task<List<CustomerOrder>> GetByStatusAsync(EnumOrderStatus enumOrderStatus)
        {
            var orders = orderMemory.Where(o => o.GetOrderStatus() == enumOrderStatus).ToList();

            return orders;
        }
        public async Task AddCustomerOrder(CustomerOrder customerOrder)
        {
            orderMemory.Add(customerOrder);
        }

       public Task<List<CustomerOrder>> SortOrdersByPriorityAndDateAsync(List<CustomerOrder> customeOrderToSort)
        {
            var sortedOrders = customeOrderToSort
                .OrderByDescending(o => o.Priority)
                .ThenBy(o => o.OrderDate)
                .ToList();

            return Task.FromResult(sortedOrders);
        }

        public async Task<List<CustomerOrder>> GetNewOrdersAsync(EnumOrderStatus enumOrderStatus)
        {
            var OrdersTask = GetByStatusAsync(enumOrderStatus);
            var newOrders = await OrdersTask; 

            return await SortOrdersByPriorityAndDateAsync(newOrders);
        }
    }
}
