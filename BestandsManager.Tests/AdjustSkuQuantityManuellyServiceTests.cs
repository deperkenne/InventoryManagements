using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestandsManager.Tests
{

   
    public class AdjustSkuQuantityManuellyServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IStockRepository> _mockStockRepository;
        private readonly Mock<IEventStoreRepository> _mockEventRepository;
        private readonly Mock<IOrderAllocationService> _mockOrderAllocationService;
        private readonly Mock<AdjustSkuQuantityManuellyService> _mockAdjustSkuQuantityManuellyService;

        public AdjustSkuQuantityManuellyServiceTests()
        {
            _mockOrderRepository = new();
            _mockStockRepository = new();
            _mockEventRepository = new();
            _mockOrderAllocationService = new();

            _mockAdjustSkuQuantityManuellyService = new (
               _mockStockRepository.Object,
               _mockOrderRepository.Object,
               _mockEventRepository.Object,
               _mockOrderAllocationService.Object
               )
            { CallBase = true };

        }



        [Fact]

        public async Task AdjustSkuQuantityManually_ShouldUpdateStockAndReallocateOrders()
        {
            // Arrange


            string skuId = "SKU_001";
            int quantityToAdd = 100;
            var affectedOrders = SetUpExpectedOrder();
            var sortedOrders = SetUpAffectedOrder();
            var orders = SetUpCustomerOrdersAndLine();
            var orderLine_001 = orders[0].GetOrderLines()[0].GetOrderId().ToString();
            var orderLine_002 = orders[1].GetOrderLines()[1].GetOrderId().ToString();
            var eventsSetUp = SetUpDomainEvents(orderLine_001, orderLine_002);

            var stockServiceMock = new Mock<AdjustSkuQuantityManuellyService>(
                _mockStockRepository.Object,
                _mockOrderRepository.Object,
                _mockEventRepository.Object,
                _mockOrderAllocationService.Object
                ) { CallBase = true };
           
            _mockStockRepository.Setup(r => r.ManuallyUpdate(skuId,quantityToAdd))
           .Returns(Task.CompletedTask);


            _mockAdjustSkuQuantityManuellyService
               .Setup(x => x.HandleStockUpdateEvent(skuId)) // ✅ correction ici
              .Returns(Task.CompletedTask)
              .Verifiable();


            /*
            _mockEventRepository.Setup(x => x.GetEventsForSkuAsync(skuId))
                .ReturnsAsync(eventsSetUp);

            _mockOrderRepository.Setup(x => x.GetAllAsListAsync())
                .ReturnsAsync(orders);

            stockServiceMock.Setup(x => x.GetAffectedOrdersFromEvents(orders, eventsSetUp))
              .Returns(affectedOrders);

            _mockOrderRepository.Setup(x => x.SortOrdersByPriorityAndDateAsync(affectedOrders))
                .ReturnsAsync(sortedOrders);
          
            stockServiceMock.Setup(x => x.TryAllocateWithFallbackAsync(sortedOrders))
                .Returns(Task.CompletedTask)
                .Verifiable();


            orderAllocationMock.Setup(s => s.AllocateStockToOrderAsync(It.IsAny<CustomerOrder>()))
                        .Returns(Task.CompletedTask);

                 orderAllocationMock.Verify(o => o.AllocateStockToOrderAsync
            (
                It.Is<CustomerOrder>
                (e => e.GetId() == "ORDER_002" && 
                 e.OrderDate == new DateTime(2025, 9, 10, 20, 40, 0) && 
                 e.IsCompleteDeliveryRequired == false && 
                 e.Priority == EnumOrderPriority.High && 
                 e.GetOrderStatus() == EnumOrderStatus.NEW)), Times.Once);
            **/

            // Act
            await _mockAdjustSkuQuantityManuellyService.Object.AdjustSkuQuantityManually(skuId, quantityToAdd);

            // Assert
       
            _mockAdjustSkuQuantityManuellyService.Verify(s => s.HandleStockUpdateEvent(skuId), Times.Once);
            _mockStockRepository.Verify(r => r.ManuallyUpdate(skuId, quantityToAdd), Times.Once);
            // Further verifications can be added based on the reallocation logic.
        }


        [Fact]
        public  async Task HandleStockUpdateEvent()
        {
            string skuId = "SKU_001";
            int quantityToAdd = 100;
            var affectedOrders = SetUpExpectedOrder();
            var sortedOrders = SetUpAffectedOrder();
            var orders = SetUpCustomerOrdersAndLine();
            var orderLine_001 = orders[0].GetOrderLines()[0].GetOrderId().ToString();
            var orderLine_002 = orders[1].GetOrderLines()[1].GetOrderId().ToString();
            var eventsSetUp = SetUpDomainEvents(orderLine_001, orderLine_002);

            _mockEventRepository.Setup(x => x.GetEventsForSkuAsync(skuId))
              .ReturnsAsync(eventsSetUp);

            _mockOrderRepository.Setup(x => x.GetAllAsListAsync())
                .ReturnsAsync(orders);

            _mockAdjustSkuQuantityManuellyService.Setup(x => x.GetAffectedOrdersFromEvents(orders, eventsSetUp))
              .Returns(affectedOrders);
            _mockOrderRepository.Setup(x => x.SortOrdersByPriorityAndDateAsync(affectedOrders))
                .ReturnsAsync(sortedOrders);

            _mockAdjustSkuQuantityManuellyService.Setup(x => x.TryAllocateWithFallbackAsync(sortedOrders))
                .Returns(Task.CompletedTask)
                .Verifiable();

            //Act
            await _mockAdjustSkuQuantityManuellyService.Object.HandleStockUpdateEvent(skuId);

            // Assert

            _mockAdjustSkuQuantityManuellyService.Verify(s => s.GetAffectedOrdersFromEvents(orders, eventsSetUp), Times.Once);
            _mockOrderRepository.Verify(s => s.SortOrdersByPriorityAndDateAsync(affectedOrders), Times.Once);
            _mockAdjustSkuQuantityManuellyService.Verify(s => s.TryAllocateWithFallbackAsync(sortedOrders), Times.Once);
            _mockOrderRepository.Verify(r => r.GetAllAsListAsync(), Times.Once);
            _mockEventRepository.Verify(r => r.GetEventsForSkuAsync(skuId), Times.Once);



        }

        [Fact]
        public async Task TryAllocateWithFallbacks()
        {
            var sortedOrders = SetUpAffectedOrder();

            //Act

            await _mockAdjustSkuQuantityManuellyService.Object.TryAllocateWithFallbackAsync(sortedOrders);

            // Assert
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
            var orders = SetUpCustomerOrdersAndLine();
            var orderLine_001 = orders[0].GetOrderLines()[0].GetOrderId().ToString(); 
            var orderLine_002 = orders[1].GetOrderLines()[1].GetOrderId().ToString(); 
            var eventsSetUp = SetUpDomainEvents(orderLine_001, orderLine_002);
           
            // Act
            var result = _mockAdjustSkuQuantityManuellyService.Object.GetAffectedOrdersFromEvents(orders,eventsSetUp);

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
        public List<DomainEvent> SetUpDomainEvents(string orderLine_001,string orderLine_002)
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
            customerOrder02.SetOrderLineItems(orderLineItems); // add oderlines to order
            customerOrders.Add(customerOrder01);  // add customerOrder to customer List
            customerOrders.Add(customerOrder02);

            return customerOrders;
        }


        public List<CustomerOrder> SetUpAffectedOrder()
        {
            List<CustomerOrder> customerOrders = new List<CustomerOrder>();

            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001", new DateTime(2025, 9, 10, 20, 40, 2), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            CustomerOrder customerOrder02 = new CustomerOrder("ORDER_002", new DateTime(2025, 9, 10, 20, 40, 0), false, EnumOrderPriority.High, EnumOrderStatus.NEW);
            customerOrders.Add(customerOrder02); 
            customerOrders.Add(customerOrder01);
            return customerOrders;
        }

        public List<CustomerOrder> SetUpCustomerOrdersAndLine()
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
