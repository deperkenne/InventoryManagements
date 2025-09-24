using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class OrderCancellationService
    {

        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;

        public OrderCancellationService(
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
        public async Task CancelOrder(string orderId)
        {

            //var cancelEvent = new OrderCancelled(orderId);

            //await _eventStoreRepository.SaveEventAsync(cancelEvent);

            Console.WriteLine($"Événement 'OrderCancelled' pour la commande '{orderId}' sauvegardé.");

            // Find all allocation events linked to this order.
            //This gives us a historical view of what has been allocated.
            var eventsToRevert = await _eventStoreRepository.GetEventsForOrderAsync(orderId);

            if (eventsToRevert.Count == 0) return;


            Console.WriteLine($"Found {eventsToRevert.Count} allocation events to revert.");

            await CreateAndSaveCancellationEventAsync(eventsToRevert);


            /**
            foreach (var allocationEvent in eventsToRevert)
            {
                if (allocationEvent is SkuQuantityAllocated skuQuantityAllocated)
                {
                    var deallocationEvent = new SkuQuantityDeallocated(skuQuantityAllocated.SkuId, skuQuantityAllocated.Quantity, skuQuantityAllocated.OrderId, skuQuantityAllocated.lineNumber);
                    await _eventStoreRepository.SaveEventAsync(deallocationEvent);
                    await DeallocatedSkuQuantyAsync(skuQuantityAllocated.SkuId, skuQuantityAllocated.Quantity);

                }


            }
            **/

        }




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

        /// <summary>
        /// Cancels a list of orders.
        /// </summary>
        public async Task CancelMultipleOrders(List<string> orderIds)
        {
            foreach (var orderId in orderIds)
            {
                await CancelOrder(orderId);
            }
        }

    }

}
