using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class OrderCancellationServiceImpl: IOrderCancellationService
    {
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;

        public OrderCancellationServiceImpl(
            IEventStoreRepository eventStoreRepository,
            IOrderRepository orderRepository,
            IStockRepository stockRepository)
        {
            _eventStoreRepository = eventStoreRepository;
            _orderRepository = orderRepository;
            _stockRepository = stockRepository;
        }

        /// <summary>
        /// Cancels a specific order and de-allocates its stock.
        /// This method serves as the main entry point for order cancellation.
        /// </summary>
        public async Task CancelOrderAsync(string orderId)
        {
            // Find all allocation events linked to this order.
            //This gives us a historical view of what has been allocated.
            var eventsToRevert = await _eventStoreRepository.GetEventsForOrderAsync(orderId);

            if (eventsToRevert.Count == 0) return;

            Console.WriteLine($"Found {eventsToRevert.Count} allocation events to revert.");
            await CreateAndSaveCancellationEventAsync(eventsToRevert);
        }


        /// <summary>
        /// Reverts a list of allocation events by creating corresponding deallocation events
        /// and rolling back the allocated stock quantities.
        /// </summary>
        /// <param name="eventsToRevert">The list of domain events to be reverted.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateAndSaveCancellationEventAsync(List<DomainEvent> eventsToRevert)
        {
            foreach (var allocationEvent in eventsToRevert)
            {
                if (allocationEvent is SkuQuantityAllocated skuQuantityAllocated)
                {
                    await _eventStoreRepository.SaveEventAsync(new SkuQuantityDeallocated(skuQuantityAllocated.SkuId, skuQuantityAllocated.QuantityAllocated, skuQuantityAllocated.OrderId, skuQuantityAllocated.LineNumber));
                    await _stockRepository.RollBackQuantityAsync(skuQuantityAllocated.SkuId, skuQuantityAllocated.QuantityAllocated);
                }
            }
        }

    }

}
