using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class AdjustSkuQuantityManuellyServiceImpl : IAdjustSkuQuantityManuellyService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IOrderAllocationService _orderAllocationService;

        public AdjustSkuQuantityManuellyServiceImpl(
                IStockRepository stockRepository,
                IOrderRepository orderRepository,
                IEventStoreRepository eventStoreRepository,
                IOrderAllocationService orderAllocationService
            )
        {
            _stockRepository = stockRepository;
            _orderRepository = orderRepository;
            _eventStoreRepository = eventStoreRepository;
            _orderAllocationService = orderAllocationService;
        }

        /// <summary>
        /// Adjusts the quantity of a specific SKU and triggers the reallocation process for affected orders.
        /// </summary>
        /// <param name="skuId">The unique identifier of the SKU.</param>
        /// <param name="newQuantity">The new manual quantity to set for the SKU.</param>
        public async Task AdjustSkuQuantityManuallyAsync(string skuId, int newQuantity)
        {
            await _stockRepository.ManuallyUpdate(skuId, newQuantity);
            // Reallocate stock for affected orders.
            await HandleStockUpdateEvent(skuId);
        }

        /// <summary>
        /// Handles the stock update event by re-evaluating and reallocating stock for orders associated with the SKU.
        /// </summary>
        /// <param name="skuId">The SKU identifier.</param>
        public async Task HandleStockUpdateEvent(string skuId)
        {
            var orders = await _orderRepository.GetAllAsListAsync();
            var events = await _eventStoreRepository.GetEventsForSkuAsync(skuId);
            var affectedOrders = GetAffectedOrdersFromEvents(orders, events);

            if (affectedOrders == null)
            {
                // No orders to reallocate, exit gracefully.
                return;
            }

            //var sortOrder = _orderAllocationService.SortOrdersByPriorityAndDate(affectedOrders);
            var sortOrder = await _orderRepository.SortOrdersByPriorityAndDateAsync(affectedOrders);
            await TryAllocateWithFallbackAsync(sortOrder);
        }

        /// <summary>
        /// Filters and returns the customer orders affected by a list of domain events,
        /// specifically <see cref="SkuQuantityAllocated"/> events.
        /// Each affected order contains only the relevant order lines.
        /// </summary>
        /// <param name="orders">The list of all customer orders to filter from.</param>
        /// <param name="events">The list of domain events that may affect some orders.</param>
        /// <returns>
        /// A list of customer orders, each containing only the order lines that are affected by the events.
        /// </returns>
        public List<CustomerOrder> GetAffectedOrdersFromEvents(List<CustomerOrder> orders, List<DomainEvent> events)
        {
            var affectedLineItemsByOrder = events
                .OfType<SkuQuantityAllocated>()
                .GroupBy(e => e.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.LineNumber).Distinct().ToList()
                );
            var affectedOrders = new List<CustomerOrder>();

            foreach (var kvp in affectedLineItemsByOrder)
            {
                var orderId = kvp.Key;
                var lineNumbers = kvp.Value;
                var order = orders.FirstOrDefault(o => o.GetId() == orderId);

                if (order != null)
                {
                    var filteredLines = order.GetOrderLines()
                                             .Where(l => lineNumbers.Contains(l.GetOrderId()))
                                             .ToList();

                    if (filteredLines.Any())
                    {
                        var clonedOrder = new CustomerOrder(order.GetId(), order.OrderDate, order.IsCompleteDeliveryRequired, order.Priority, order.GetOrderStatus());

                        foreach (var line in filteredLines)
                        {
                            clonedOrder.AddOrderLine(line);
                        }

                        affectedOrders.Add(clonedOrder);
                    }
                }
            }

            return affectedOrders;
        }

        /// <summary>
        /// Attempts to allocate stock for each customer order in the provided sorted list.
        /// Calls the default allocation logic for every order in the list.
        /// </summary>
        /// <param name="sortCustomerOrders">List of sorted customer orders to process for stock allocation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TryAllocateWithFallbackAsync(List<CustomerOrder> sortCustomerOrders)
        {
            foreach (var order in sortCustomerOrders)
            {
                await _orderAllocationService.AllocateStockToOrderAsync(order);
            }
        }
    }
}
