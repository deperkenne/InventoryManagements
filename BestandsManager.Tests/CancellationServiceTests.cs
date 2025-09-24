using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;
using Moq;

namespace BestandsManager.Tests
{
    public class CancellationServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IStockRepository> _mockStockRepository;
        private readonly Mock<IEventStoreRepository> _mockEventRepository;
        private readonly OrderCancellationService _cancellationService;


        public CancellationServiceTests()
        {
            _mockOrderRepository = new();
            _mockStockRepository = new();
            _mockEventRepository = new();

            _cancellationService = new OrderCancellationService(
                _mockEventRepository.Object,
                _mockOrderRepository.Object,
                _mockStockRepository.Object);
        }


        [Fact]
        public async Task CreateAndSaveCancellationEventAsync_SavesDeallocationEvent_WhenInputContainsSkuQuantityAllocatedEvents()
        {
            CustomerOrder customerOrder01 = new CustomerOrder("ORDER_001",new DateTime(2025, 9, 10, 20, 40, 0), true, EnumOrderPriority.High, EnumOrderStatus.NEW);
            var mockDomainEvent = new List<DomainEvent>();
            SkuQuantityAllocated allocatedEvent1 = new SkuQuantityAllocated("SKU_001", 30, customerOrder01.GetId(), "allocation", "01", EnumOrderStatus.RELEASED);
            SkuQuantityAllocated allocatedEvent2 = new SkuQuantityAllocated("SKU_002", 20, customerOrder01.GetId(), "allocation", "01", EnumOrderStatus.RELEASED);
            mockDomainEvent.Add(allocatedEvent1);
            mockDomainEvent.Add(allocatedEvent2);


            // Act
            await _cancellationService.CreateAndSaveCancellationEventAsync(mockDomainEvent);


            // Assert
            _mockEventRepository.Verify(r => r.SaveEventAsync(It.Is<SkuQuantityDeallocated>(e => e.SkuId == "SKU_001" && e.QuantityDeallocated == 30 && e.OrderId == customerOrder01.GetId() && e.LineNumber == "01")), Times.Once);
            _mockEventRepository.Verify(r => r.SaveEventAsync(It.Is<SkuQuantityDeallocated>(e => e.SkuId == "SKU_002" && e.QuantityDeallocated == 20 && e.OrderId == customerOrder01.GetId() && e.LineNumber == "01")), Times.Once);
            _mockStockRepository.Verify(r => r.RollBackQuantityAsync("SKU_001", 30), Times.Once);
            _mockStockRepository.Verify(r => r.RollBackQuantityAsync("SKU_002", 20), Times.Once);
        }


        [Fact]
        public async Task CancelOrder_ShouldNotCancel_WhenOrderIsAlreadyCancelled()
        {
            // Arrange
            var expectedEvents = new List<DomainEvent>();

            _mockEventRepository.Setup(r => r.GetEventsForOrderAsync("ORDER_009"))
                 .ReturnsAsync(expectedEvents);
            // Act
            await _cancellationService.CancelOrder("ORDER_009");
            // Assert
            _mockEventRepository.Verify(r => r.SaveEventAsync(It.IsAny<DomainEvent>()), Times.Never);
            _mockStockRepository.Verify(r => r.RollBackQuantityAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);

        }

    }
}
