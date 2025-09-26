using InventoryManagement.Logistics.Execptions;
using InventoryManagement.Logistics.Logics;
using InventoryManagement.Logistics.Logics.Impl;
using InventoryManagement.Logistics.Models;
using InventoryManagement.Logistics.Repositories;
using Moq;

namespace InventotyManagement.Tests
{
    public class OrderAllocationServiceImplTests
    {
        private List<CustomerOrder> expectedOrders;
        private readonly Mock<IOrderRepository> mockOrderRepository;
        private readonly Mock<IStockRepository> mockStockRepository;
        private readonly Mock<IEventStoreRepository> mockEventRepository;
        private readonly Mock<IOrderCancellationService> mockOrderCancellation;
        private readonly OrderAllocationServiceImpl orderAllocationService;
      
        public OrderAllocationServiceImplTests()
        {
            mockOrderRepository = new();
            mockStockRepository = new();
            mockEventRepository = new();
            mockOrderCancellation = new();

            orderAllocationService = new OrderAllocationServiceImpl
             (
                mockOrderRepository.Object,
                mockStockRepository.Object,
                mockEventRepository.Object,
                mockOrderCancellation.Object
            );     
        }

        [Fact]
        public async Task AllocateStockToOrderAsync_FailsCompleteDelivery_WhenNoSkuFound()
        {
            // Arrange
            var customerOrders = GetOrdersWithDifferentLineConfigurations();
            var customerOrder = customerOrders[0];
            mockStockRepository.Setup(repo =>  repo.GetAvailableSkusAsync("TV")).ReturnsAsync((List<Sku>)null);

            //Act && Assert 
            await Assert.ThrowsAsync<CompleteDeliverySkuNotFoundException>(() =>
              orderAllocationService.AllocateStockToOrderAsync(customerOrder)
            );
        }

        [Fact]
        public async Task TryCreateAllocationsAsync_IgnoresLine_WhenNoSkusAndPartialDeliveryAllowed()
        {
            // Arrange
            var customerOrders = GetOrdersWithDifferentLineConfigurations();
            var customerOrder = customerOrders[1];
            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("TV")).ReturnsAsync((List<Sku>)null);

            // Act
            var exception = await Record.ExceptionAsync(() => orderAllocationService.AllocateStockToOrderAsync(customerOrder));

            // Assert
            Assert.Null(exception);     
        }

        [Fact]
        public async Task AllocateStockToOrderAsync_FullyAllocatesQuantity_UsingMultipleSkus()
        {
            // Arrange
            var expectedSkus = GetSkuByProductName();
            var customerOrder = CreateOrderWithSingleLine(true, 600);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);

            // Act
             await orderAllocationService.AllocateStockToOrderAsync(customerOrder);

            // Assert
            Assert.Equal("SKU_004", orderAllocationService.pendingAllocations[0].sku.SkuId);
            Assert.Equal(300, orderAllocationService.pendingAllocations[0].quantity);
            Assert.Equal("SKU_005", orderAllocationService.pendingAllocations[1].sku.SkuId);
            Assert.Equal(300, orderAllocationService.pendingAllocations[1].quantity);
        }

        [Fact]
        public async Task TryCreateAllocationsAsync_Should_Succeed_And_SaveEvents_When_NoCompleteDeliveryRequired()
        {
            // Arrange
            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1",600, "SKU_001");
            OrderLineItem orderLineItem02 = new OrderLineItem("WATER_L1", 600, "SKU_001");
            orderLineItems.Add(orderLineItem01);
            CustomerOrder customerOrder = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder.SetOrderLineItems(orderLineItems); 

            List<Sku> skus = new List<Sku>();
            Sku sku_01 = new Sku("SKU_001", "COLA_L1", "Shelf 1", false, 100);
            Sku sku_02 = new Sku("SKU_002", "COLA_L1", "Shelf 2", true, 200);
            Sku sku_03 = new Sku("SKU_003", "FANTA_L1", "Shelf 2", false, 200);
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_05 = new Sku("SKU_005", "WATER_L1", "Shelf 4", false, 600);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 250);
            skus.Add(sku_01);
            skus.Add(sku_02);
            skus.Add(sku_03);
            skus.Add(sku_04);
            skus.Add(sku_05);
            skus.Add(sku_06);

            var pendingAllocations = new List<(Sku sku, int quantity, string line)>
            {
                (new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300), 300, "Line-A"),
                (new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 250), 250, "Line-B")
            };

            var remainingQuantity = 50;

            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, remainingQuantity, pendingAllocations);

            // Assert
            Assert.True(result); 

            mockEventRepository.Verify(
                r => r.SaveEventAsync(It.Is<SkuQuantityAllocated>(
                    e => e.SkuId == "SKU_004" && e.QuantityAllocated == 300 && e.OrderId == "ORDER_001" && e.LineNumber == "Line-A")),
                Times.Once
            );

            mockEventRepository.Verify(
                r => r.SaveEventAsync(It.Is<SkuQuantityAllocated>(
                    e => e.SkuId == "SKU_005" && e.QuantityAllocated == 250 && e.OrderId == "ORDER_001" && e.LineNumber == "Line-B")),
                Times.Once
            );

      
            mockOrderRepository.Verify(
                r => r.UpdateStatusAsync(customerOrder),
                Times.Once
            );

            mockStockRepository.Verify(r => r.ApplyAllocationsAsync(pendingAllocations),
                Times.Once);
        }

        [Fact]
        public async Task TryCreateAllocationsAsync_Should_Fail_When_CompleteDeliveryRequired_And_NotFulfilled()
        {
            // Arrange
            var order = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            var remainingQuantity = 5; 
            var pendingAllocations = new List<(Sku sku, int quantity, string line)>();

            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(order, remainingQuantity, pendingAllocations);

            // Assert
            Assert.False(result); 

            mockOrderCancellation.Verify(
                r => r.CancelOrderAsync("ORDER_001"),
                Times.Once
            );

            mockEventRepository.Verify(
                r => r.SaveEventAsync(It.IsAny<SkuQuantityAllocated>()),
                Times.Never()
            );

            mockStockRepository.Verify(
                r => r.ApplyAllocationsAsync(It.IsAny<List<(Sku sku, int quantity, string line)>>()),
                Times.Never()
            );

            mockOrderRepository.Verify(
                r => r.UpdateStatusAsync(It.IsAny<CustomerOrder>()),
                Times.Never()
            );
        }

        [Fact]
        public async Task AllocateStockToOrderAsync_SkipsLockedSku_AndAllocatesFromAvailable()
        {
            // Arrange
            var customerOrder = CreateOrderWithSingleLine(true, 300);
            List<Sku> expectedSkus = new List<Sku>();

            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", true, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            expectedSkus.Add(sku_04);
            expectedSkus.Add(sku_06);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);

            // Act
            await orderAllocationService.AllocateStockToOrderAsync(customerOrder);

            // Assert
            Assert.Single(orderAllocationService.pendingAllocations);
            Assert.Equal("SKU_005", orderAllocationService.pendingAllocations[0].sku.SkuId);
            Assert.Equal(300, orderAllocationService.pendingAllocations[0].quantity);
        }



        //helper methode 
        public CustomerOrder CreateOrderWithSingleLine(bool isCompleteDelivery, int quantity)
        {
            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", quantity, "SKU_001");
            orderLineItems.Add(orderLineItem01);
            CustomerOrder customerOrder = new CustomerOrder("ORDER_001",new DateTime(2025, 9, 10, 20, 40, 2), isCompleteDelivery, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder.SetOrderLineItems(orderLineItems); // add

            return customerOrder;
        }

        public List<CustomerOrder> SetUpCustomerOrders()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 0), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder order005 = new CustomerOrder("ORDER_003", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.Normal);
            CustomerOrder order004 = new CustomerOrder("ORDER_004", new DateTime(2025, 9, 10, 20, 40, 4), true, EnumOrderPriority.Normal);
            CustomerOrder Order003 = new CustomerOrder("ORDER_005", new DateTime(2025, 9, 10, 20, 40, 3), true, EnumOrderPriority.Low);

            customerOrders.Add(order004);
            customerOrders.Add(customerOrder02);
            customerOrders.Add(Order003);
            customerOrders.Add(customerOrder01);
            customerOrders.Add(order005);

            return customerOrders;
        }

        public List<Sku> SetUpSkuList()
        {
            List<Sku> skus = new List<Sku>();
            Sku sku_01 = new Sku("SKU_001", "COLA_L1", "Shelf 1", false, 100);
            Sku sku_02 = new Sku("SKU_002", "COLA_L1", "Shelf 2", true, 200);
            Sku sku_03 = new Sku("SKU_003", "FANTA_L1", "Shelf 2", false, 200);
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_05 = new Sku("SKU_005", "WATER_L1", "Shelf 4", false, 400);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 250);

            skus.Add(sku_01);
            skus.Add(sku_02);
            skus.Add(sku_03);
            skus.Add(sku_04);
            skus.Add(sku_05);
            skus.Add(sku_06);

            return skus;
        }

        public List<Sku> GetSkuByProductName()
        {
            List<Sku> skus = new List<Sku>();
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            skus.Add(sku_04);
            skus.Add(sku_06);

            return skus;
        }

        public List<CustomerOrder> GetOrdersWithDifferentLineConfigurations()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", 30, "SKU_001");
            OrderLineItem orderLineItem02 = new OrderLineItem("FANTA_L1", 30,  "SKU_001");
            orderLineItems.Add(orderLineItem01);
            orderLineItems.Add(orderLineItem02);
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder01.SetOrderLineItems(orderLineItems); // add oderlines to order


            OrderLineItem orderLineItem04 = new OrderLineItem("ORANGE_L1", 30, "SKU_001");
            OrderLineItem orderLineItem05 = new OrderLineItem("FANTA_L1", 30, "SKU_001");
            orderLineItems.Add(orderLineItem04);
            orderLineItems.Add(orderLineItem05);
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder02.SetOrderLineItems(orderLineItems); // add oderlines to order

            OrderLineItem orderLineItem06 = new OrderLineItem("ORANGE_L1", 30,  "SKU_001");
            OrderLineItem orderLineItem07 = new OrderLineItem("FANTA_L1", 30, "SKU_001");
            OrderLineItem orderLineItem08 = new OrderLineItem("FANTA_L1", 30,  "SKU_001");
            orderLineItems.Add(orderLineItem06);
            orderLineItems.Add(orderLineItem07);
            orderLineItems.Add(orderLineItem08);
            CustomerOrder Order003 = new CustomerOrder("ORDER_003", new DateTime(2025, 9, 10, 20, 40, 3), true, EnumOrderPriority.Low);
            Order003.SetOrderLineItems(orderLineItems); // add oderlines to order

            CustomerOrder order004 = new CustomerOrder("ORDER_004", new DateTime(2025, 9, 10, 20, 40, 4), true, EnumOrderPriority.High);
            CustomerOrder order005 = new CustomerOrder("ORDER_004", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.Normal);

            customerOrders.Add(customerOrder01);  // add customerOrder to customer List
            customerOrders.Add(customerOrder02);
            customerOrders.Add(Order003);
            customerOrders.Add(order004);
            customerOrders.Add(order005);

            return customerOrders;
        }
    }
}

