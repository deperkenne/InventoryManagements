using InventoryManagement.Logistics.Logics;
using InventoryManagement.Logistics.Logics.Impl;
using InventoryManagement.Logistics.Models;
using InventoryManagement.Logistics.Repositories;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventotyManagement.Tests
{ 
    public class AdjustSkuQuantityManuellyServiceImplTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IStockRepository> _mockStockRepository;
        private readonly Mock<IEventStoreRepository> _mockEventRepository;
        private readonly Mock<IOrderAllocationService> _mockOrderAllocationService;
        private readonly AdjustSkuQuantityManuellyServiceImpl  adjustSkuQuantityManuellyService;
 
        public AdjustSkuQuantityManuellyServiceImplTests()
        {
            _mockOrderRepository = new();
            _mockStockRepository = new();
            _mockEventRepository = new();
            _mockOrderAllocationService = new();

             adjustSkuQuantityManuellyService = new AdjustSkuQuantityManuellyServiceImpl(
               _mockStockRepository.Object,
               _mockOrderRepository.Object,
               _mockEventRepository.Object,
               _mockOrderAllocationService.Object
               );
        }

        [Fact]
        public async Task TryAllocateWithFallbacks()
        {
            //Arrange
            var sortedOrders = CreateSampleAffectedOrders();

            //Act
            await adjustSkuQuantityManuellyService.TryAllocateWithFallbackAsync(sortedOrders);

            //Assert
            _mockOrderAllocationService.Verify(o => o.AllocateStockToOrderAsync
            (
                It.Is<CustomerOrder>
                (e => e.GetId() == "ORDER_002" &&
                 e.OrderDate == new DateTime(2025, 9, 10, 20, 40, 0) &&
                 e.IsCompleteDeliveryRequired == false &&
                 e.Priority == EnumOrderPriority.High &&
                 e.GetOrderStatus() == EnumOrderStatus.NEW)), Times.Once);
        }

        [Fact]
        public void GetAffectedOrdersFromEvents_ShouldReturnCorrectOrdersWithFilteredLines()
        {
            // Arrange
            List<DomainEvent> events = new List<DomainEvent>();
            var orders = GetOrdersWithDifferentLineConfigurations();
            var orderLine_001 = orders[0].GetOrderLines()[0].GetOrderId().ToString(); 
            var orderLine_002 = orders[1].GetOrderLines()[1].GetOrderId().ToString(); 
            var eventsSetUp = CreateSampleDomainEvents(orderLine_001, orderLine_002);
           
            // Act
            var result = adjustSkuQuantityManuellyService.GetAffectedOrdersFromEvents(orders,eventsSetUp);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(orders[0].GetId(), result.Select(o => o.GetId()));
            Assert.Contains(orders[1].GetId(), result.Select(o => o.GetId()));

            foreach (var affectedOrder in result)
            {
                Assert.Equal(1, affectedOrder.GetOrderLines().Count);
            }
        }

        // helper methode 
        public List<DomainEvent> CreateSampleDomainEvents(string orderLine_001,string orderLine_002)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            var expectedSKu_001 = new SkuQuantityAllocated("SKU_001", 50, "ORDER_001", "allocation", orderLine_001, EnumOrderStatus.RELEASED);
            var expectedSku_002 = new SkuQuantityAllocated("SKU_001", 30, "ORDER_002", "allocation", orderLine_002, EnumOrderStatus.RELEASED);
            events.Add(expectedSKu_001);
            events.Add(expectedSku_002);

            return events;
        }

        public List<CustomerOrder> SetUpExpectedOrder()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", 30, "SKU_001");
            orderLineItems.Add(orderLineItem01);
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder01.SetOrderLineItems(orderLineItems); // add oderlines to order

            OrderLineItem orderLineItem04 = new OrderLineItem("ORANGE_L1", 30, "SKU_001");
            orderLineItems.Add(orderLineItem04);
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder02.SetOrderLineItems(orderLineItems); 

            customerOrders.Add(customerOrder01);  
            customerOrders.Add(customerOrder02);

            return customerOrders;
        }

        public List<CustomerOrder> CreateSampleAffectedOrders()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrders.Add(customerOrder02); 
            customerOrders.Add(customerOrder01);

            return customerOrders;
        }

        public List<CustomerOrder> GetOrdersWithDifferentLineConfigurations()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            List<OrderLineItem> orderLineItems = new List<OrderLineItem>();
            OrderLineItem orderLineItem01 = new OrderLineItem("ORANGE_L1", 30,  "SKU_001");
            OrderLineItem orderLineItem02 = new OrderLineItem("FANTA_L1", 30,"SKU_001");
            orderLineItems.Add(orderLineItem01);
            orderLineItems.Add(orderLineItem02);
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder01.SetOrderLineItems(orderLineItems); // add oderlines to order

            OrderLineItem orderLineItem04 = new OrderLineItem("ORANGE_L1", 30, "SKU_001");
            OrderLineItem orderLineItem05 = new OrderLineItem("FANTA_L1", 30,  "SKU_002" );
            orderLineItems.Add(orderLineItem04);
            orderLineItems.Add(orderLineItem05);
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrder02.SetOrderLineItems(orderLineItems); // add oderlines to order
         
            OrderLineItem orderLineItem06 = new OrderLineItem("WATER_L1", 30,  "SKU_001");
            OrderLineItem orderLineItem07 = new OrderLineItem("ORANGE_L1", 30,  "SKU_001");
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
