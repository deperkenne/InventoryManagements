using BestandsManager.Logistics.Execptions;
using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;
using Moq;

namespace BestandsManager.Tests
{
    public class AllocationServiceTests

    {
        private List<CustomerOrder> expectedOrders;
        private readonly Mock<IOrderRepository> mockOrderRepository;
        private readonly Mock<IStockRepository> mockStockRepository;
        private readonly Mock<IEventStoreRepository> mockEventRepository;
        private readonly Mock<OrderAllocationServiceImpl> mockService;
        private OrderCancellationService orderCancellationService;
        private readonly OrderAllocationServiceImpl orderAllocationService;
        private List<(Sku sku, int quantity)> pendingAllocations;
        private List<string> lineNumbers;
        public AllocationServiceTests()
        {
            mockOrderRepository = new();
            mockStockRepository = new();
            mockEventRepository = new();
            orderCancellationService = new OrderCancellationService
                (
                    mockEventRepository.Object,
                    mockOrderRepository.Object,
                    mockStockRepository.Object
                );

            orderAllocationService = new OrderAllocationServiceImpl
             (
                mockOrderRepository.Object,
                mockStockRepository.Object,
                mockEventRepository.Object,
                orderCancellationService
            );

            mockService = new Mock<OrderAllocationServiceImpl>(
                mockOrderRepository.Object,
                mockStockRepository.Object,
                mockEventRepository.Object,
                orderCancellationService
            );

            pendingAllocations = new List<(Sku sku, int quantity)>();
            lineNumbers = new List<string>();

        }




        [Fact]
        public async Task ApplyAllocationsAsync_ShouldCallUpdateQuantity_ForEachAllocation()
        {
            // Arrange
            Sku sku_01 = new Sku("SKU_001", "COLA_L1", "Shelf 1", false, 100);
            Sku sku_02 = new Sku("SKU_002", "COLA_L1", "Shelf 2", true, 200);
            Sku sku_03 = new Sku("SKU_003", "FANTA_L1", "Shelf 2", false, 200);

            var allocations = new List<(Sku sku, int quantity)>();

            allocations.Add((sku_01, 10));
            allocations.Add((sku_02, 20));
            allocations.Add((sku_03, 30));

            // Act
            await orderAllocationService.ApplyAllocationsAsync(allocations);

            // Assert
            mockStockRepository.Verify(r => r.UpdateQuantityAsync("SKU_001", 10), Times.Once);
            mockStockRepository.Verify(r => r.UpdateQuantityAsync("SKU_002", 20), Times.Once);
            mockStockRepository.Verify(r => r.UpdateQuantityAsync("SKU_003", 30), Times.Once);
        }

        [Fact]
        public void SortOrdersByPriorityAndDate_SortsCorrectly()
        {
            // Arrange

            var orders = SetUpCustomerOrders();

            // Act
            var sortedOrders = orderAllocationService.SortOrdersByPriorityAndDate(orders);

            // Assert
            Assert.Equal(5, sortedOrders.Count);

            // Verify priority order (High, High, Normal, Normal, Low)
            Assert.Equal(EnumOrderPriority.High, sortedOrders[0].Priority);
            Assert.Equal(EnumOrderPriority.High, sortedOrders[1].Priority);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 0), sortedOrders[0].OrderDate);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 2), sortedOrders[1].OrderDate);


        }



        [Fact]
        public async Task GetNewOrdersAsync_ShouldCallRepositoryAndReturnSortedOrders()
        {
            // Arrange

            var expectedOrders = SetUpCustomerOrdersAndLine();
            mockOrderRepository.Setup(repo => repo.GetByStatusAsync(EnumOrderStatus.NEW)).ReturnsAsync(expectedOrders);

            // Act

            var sortedOrders = await orderAllocationService.GetNewOrdersAsync(EnumOrderStatus.NEW);

            // Assert

            mockOrderRepository.Verify(repo => repo.GetByStatusAsync(EnumOrderStatus.NEW), Times.Once);

            Assert.Equal(EnumOrderPriority.High, sortedOrders[0].Priority);
            Assert.Equal(EnumOrderPriority.High, sortedOrders[1].Priority);
            Assert.Equal(EnumOrderPriority.High, sortedOrders[2].Priority);
            Assert.Equal(EnumOrderPriority.Normal, sortedOrders[3].Priority);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 0), sortedOrders[0].OrderDate);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 2), sortedOrders[1].OrderDate);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 4), sortedOrders[2].OrderDate);
            Assert.Equal(new DateTime(2025, 9, 10, 20, 40, 2), sortedOrders[3].OrderDate);


        }

        [Fact]
        public async Task TryCreateAllocationsAsync_ShouldThrow_WhenSkusNull_AndCompleteDeliveryRequired()
        {
            // Arrange

            var customerOrders = SetUpCustomerOrdersAndLine();

            var customerOrder = customerOrders[0];

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("WATER_L1")).ReturnsAsync((List<Sku>)null);

            // Act & Assert
            await Assert.ThrowsAsync<CompleteDeliverySkuNotFoundException>(() =>
               orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers)
            );


        }

        [Fact]
        public async Task TryCreateAllocationsAsync_IgnoresLine_WhenNoSkusAndPartialDeliveryAllowed()
        {

            // Arrange

            var customerOrders = SetUpCustomerOrdersAndLine();

            var customerOrder = customerOrders[1];

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("WATER_L1")).ReturnsAsync((List<Sku>)null);

            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers);

            // Assert
            Assert.True(result);
            Assert.Empty(pendingAllocations);

        }

        [Fact]
        public async Task TryCreateAllocationsAsync_StopsProcessingLine_WhenRemainingQuantityIsZeroOrLess()
        {
            // Arrange
            var expectedSkus = GetSkuByProductName();
            var customerOrder = ChangeOrderIsLockedAndOrderLineQuantity(true, 300);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);
            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers);

            // Assert
            Assert.True(result);
            Assert.Single(pendingAllocations);
            Assert.Equal("SKU_004", pendingAllocations[0].sku.SkuId);
            Assert.Equal(300, pendingAllocations[0].quantity);


        }


        [Fact]
        public async Task TryCreateAllocationsAsync_ShouldSkipLockedSkus_WhenProcessingAvailableSkus()
        {
            List<Sku> expectedSkus = new List<Sku>();

            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", true, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            expectedSkus.Add(sku_04);
            expectedSkus.Add(sku_06);

            var customerOrder = ChangeOrderIsLockedAndOrderLineQuantity(true, 300);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);
            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers);
            Assert.True(result);
            Assert.Single(pendingAllocations);
            Assert.Equal("SKU_005", pendingAllocations[0].sku.SkuId);
            Assert.Equal(300, pendingAllocations[0].quantity);


        }



        [Fact]
        public async Task TryCreateAllocationsAsync_ReturnsFalse_WhenRemainingQuantityIsPositive_AndCompleteDeliveryIsRequired()
        {
            List<Sku> expectedSkus = new List<Sku>();
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            expectedSkus.Add(sku_04);
            expectedSkus.Add(sku_06);
            var customerOrder = ChangeOrderIsLockedAndOrderLineQuantity(true, 800);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);

            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers);

            // Assert
            Assert.False(result);


        }

        [Fact]
        public async Task TryCreateAllocationsAsync_ReturnsTrue_WhenRemainingQuantityIsPositive_AndCompleteDeliveryIsRequired()
        {
            List<Sku> expectedSkus = new List<Sku>();
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            expectedSkus.Add(sku_04);
            expectedSkus.Add(sku_06);
            var customerOrder = ChangeOrderIsLockedAndOrderLineQuantity(true, 600);

            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);

            // Act
            var result = await orderAllocationService.TryCreateAllocationsAsync(customerOrder, pendingAllocations, lineNumbers);

            // Assert
            Assert.True(result);


        }



        [Fact]

        public async Task AllocateStockToOrderAsync_AllocatesStock_WhenSufficientStockIsAvailable()
        {
            List<Sku> expectedSkus = new List<Sku>();
            Sku sku_04 = new Sku("SKU_004", "ORANGE_L1", "Shelf 3", false, 300);
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
            expectedSkus.Add(sku_04);
            expectedSkus.Add(sku_06);
            var customerOrder = ChangeOrderIsLockedAndOrderLineQuantity(true, 600);


            mockStockRepository.Setup(repo => repo.GetAvailableSkusAsync("ORANGE_L1")).ReturnsAsync(expectedSkus);

            // Act

            await orderAllocationService.AllocateStockToOrderAsync(customerOrder);


            // Assert
            mockEventRepository.Verify(r => r.SaveEventAsync(It.IsAny<SkuQuantityAllocated>()), Times.Exactly(2));
            mockEventRepository.Verify(r => r.SaveEventAsync(It.Is<SkuQuantityAllocated>(e => e.SkuId == "SKU_004" && e.QuantityAllocated == 300 && e.OrderId == customerOrder.GetId() && e.EventType == "allocation" && e.LineNumber == customerOrder.GetOrderLines()[0].GetOrderId())), Times.Once);
            mockEventRepository.Verify(r => r.SaveEventAsync(It.Is<SkuQuantityAllocated>(e => e.SkuId == "SKU_005" && e.QuantityAllocated == 300 && e.OrderId == customerOrder.GetId() && e.EventType == "allocation" && e.LineNumber == customerOrder.GetOrderLines()[0].GetOrderId())), Times.Once);
            mockStockRepository.Verify(r => r.UpdateQuantityAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
            mockStockRepository.Verify(r => r.UpdateQuantityAsync("SKU_004", 300), Times.Once);
            mockStockRepository.Verify(r => r.UpdateQuantityAsync("SKU_005", 300), Times.Once);
            mockOrderRepository.Verify(r => r.UpdateStatusAsync(It.Is<CustomerOrder>(o => o.GetId() == customerOrder.GetId()
            && o.GetOrderStatus() == EnumOrderStatus.NEW && o.OrderDate == customerOrder.OrderDate && o.Priority == EnumOrderPriority.High)), Times.Once);


        }




        //helper methode to change orderlineItem attribute

        public CustomerOrder ChangeOrderIsLockedAndOrderLineQuantity(bool isLocked, int quantity)
        {
            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", quantity, "SKU_001");
            orderLineItems.Add(orderLineItem01);
            CustomerOrder customerOrder = new CustomerOrder("ORDER_001",new DateTime(2025, 9, 10, 20, 40, 2), isLocked, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder.SetOrderLineItems(orderLineItems); // add

            return customerOrder;

        }


        // helper methode to set up customer list
        public List<CustomerOrder> SetUpCustomerOrders()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 0), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder order005 = new CustomerOrder("ORDER_003",new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.Normal);
            CustomerOrder order004 = new CustomerOrder("ORDER_004", new DateTime(2025, 9, 10, 20, 40, 4), true, EnumOrderPriority.Normal);
            CustomerOrder Order003 = new CustomerOrder("ORDER_005", new DateTime(2025, 9, 10, 20, 40, 3), true, EnumOrderPriority.Low);


            customerOrders.Add(customerOrder01);  // add customerOrder to customer List
            customerOrders.Add(customerOrder02);
            customerOrders.Add(Order003);
            customerOrders.Add(order004);
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
            Sku sku_06 = new Sku("SKU_005", "ORANGE_L1", "Shelf 4", false, 400);
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

        // helper methode to set up customer list and his orderlines
        public List<CustomerOrder> SetUpCustomerOrdersAndLine()
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

