using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repositories.Impl;
using BestandsManager.Logistics.Repository;
using Moq;

namespace BestandsManager.Tests
{
    public class OrderServiceImplTests
    {
        private List<CustomerOrder> custormerOrders;
        private InMemoryOrderImpl orderService;
        private List<CustomerOrder> expectedOrders;
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IStockRepository> _mockStockRepository;
        private readonly Mock<IEventStoreRepository> _mockEventRepository;
        private readonly Mock<OrderAllocationServiceImpl> mockService;
        private OrderCancellationService orderCancellationService;
        private readonly OrderAllocationServiceImpl orderAllocationService;
        public OrderServiceImplTests()
        {

            _mockOrderRepository = new();
            custormerOrders = new List<CustomerOrder>();
            orderService = new InMemoryOrderImpl();
            //orderService.InitializeOrders();

        }


        [Fact]
        public async Task InitializeOrders_ShouldCreateTwoOrders_WhenCalled()
        {
            //ASSERT
            Assert.Equal(2, orderService.orderMemory.Count);

        }

        [Theory]
        [InlineData(EnumOrderPriority.High, 0)]
        [InlineData(EnumOrderPriority.High, 1)]

        public void InitializeOrders_ShouldCreateOrdersWithCorrectPriority(EnumOrderPriority orderPriority, int index)
        {

            //ACT
            Assert.Equal(orderPriority, orderService.orderMemory[index].Priority);

        }


        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void InitializeOrders_ShouldCreateOrdersWithCorrectOrderLine(int index)
        {
            var expectedLines = SetUpCustomerOrdersAndLine()[index].GetOrderLines();
            var actualLines = orderService.orderMemory[index].GetOrderLines();

            // Assert
            Assert.True(expectedLines.SequenceEqual(actualLines));

        }

        [Fact]
        public async Task UpdateStatusAsync_WhenOrderExists_ShouldUpdateStatusToReleased()
        {

            //Arrange

            var customerOrder = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High);

            // Act

            await orderService.UpdateStatusAsync(customerOrder);

            // Assert

            Assert.Equal(EnumOrderStatus.RELEASED, orderService.orderMemory[0].GetOrderStatus());
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenOrderDoesNotExist_ShouldThrowException()
        {
            // Arrange
            CustomerOrder expectedOrder001 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High);

            //Act

            var result = await orderService.GetByIdAsync("ORDER_001");

            // Assert
            Assert.Equal(expectedOrder001.GetId(), result.GetId());

        }


        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
        {
            // Arrange
            string emptyId = string.Empty;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await orderService.GetByIdAsync(emptyId));
        }

        [Fact]

        public async Task GetByStatusAsync_ShouldReturnOrdersWithGivenStatus()
        {
            // Arrange
            var expectedStatus = EnumOrderStatus.NEW;
            orderService.orderMemory.Add(new CustomerOrder("ORDER_003", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.RELEASED));
            // Act
            var actualOrders = await orderService.GetByStatusAsync(expectedStatus);
            // Assert
            Assert.Equal(2, actualOrders.Count);

        }

        [Fact]
        public async Task GetAllAsListAsync_ShouldReturnAllOrders()
        {
            // Arrange
            var expectedOrders = 2;
            // Act
            var actualOrders = await orderService.GetAllAsListAsync();
            // Assert
            Assert.Equal(expectedOrders, actualOrders.Count);

        }


        [Theory]
        [MemberData(nameof(OrderData))]

        public async Task AddCustomerOrder_ShouldAddNewOrder(CustomerOrder order, int index)
        {
            // Arrange
            int initialCount = orderService.orderMemory.Count ;

            var expectedOrder = order;

            //Act
            await orderService.AddCustomerOrder(order);

            // Assert

            Assert.Equal(expectedOrder.GetId(), orderService.orderMemory[initialCount].GetId());

        }



        // helper methode
        public async Task<List<CustomerOrder>> Setup(InMemoryOrderImpl orderServiceImpl)
        {
            orderService.InitializeOrders();
            var orderResult = await orderService.GetAllAsListAsync();
            return orderResult;

        }


        public List<CustomerOrder> SetUpCustomerOrdersAndLine()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", 30,  "SKU_002" );
            OrderLineItem orderLineItem02 = new OrderLineItem("WATER_L1", 50,  "SKU_001");

            orderLineItems.Add(orderLineItem01);
            orderLineItems.Add(orderLineItem02);


            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High);
            customerOrder01.SetOrderLineItems(orderLineItems); // add oderlines to order


            OrderLineItem orderLineItem04 = new OrderLineItem("ORANGE_L1", 50,  "SKU_001");
            OrderLineItem orderLineItem05 = new OrderLineItem("WATER_L1", 30,  "SKU_001");
            orderLineItems.Add(orderLineItem04);
            orderLineItems.Add(orderLineItem05);

            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder02.SetOrderLineItems(orderLineItems);

            return customerOrders;
        }


        public static IEnumerable<object[]> OrderLineTestData =>
            new List<object[]>
            {
                new object[] { new OrderLineItem("ORANGE_L1", 30,"SKU_001"), 0 },
                new object[] { new OrderLineItem("WATER_L1", 50,  "SKU_001"), 1 }
            };

        public static IEnumerable<object[]> OrderData =>
           new List<object[]>
           {
               new object[] { new CustomerOrder("ORDER_008", new DateTime(2025, 9, 10, 20, 40, 0), true, EnumOrderPriority.High), 1 },
               new object[] { new CustomerOrder("ORDER_007", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High), 2 }

           };
    }

}
