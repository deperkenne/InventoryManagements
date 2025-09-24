using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class AdjustSkuQuantityManuellyService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IOrderAllocationService _orderAllocationService;


        public AdjustSkuQuantityManuellyService(
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
        public   async Task AdjustSkuQuantityManually(string skuId, int newQuantity)
        {
            await _stockRepository.ManuallyUpdate(skuId, newQuantity);

            // Reallocate stock for affected orders.
            await HandleStockUpdateEvent(skuId);


        }


        /// <summary>
        /// Handles the stock update event by re-evaluating and reallocating stock for orders associated with the SKU.
        /// </summary>
        /// <param name="skuId">The SKU identifier.</param>
        public virtual async Task HandleStockUpdateEvent(string skuId)
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
            var sortOrder = _orderRepository.SortOrdersByPriorityAndDateAsync(affectedOrders).Result;
            await TryAllocateWithFallbackAsync(sortOrder);

        }


        /*
        /// <param name="orders">All existing orders.</param>
        /// <param name="events">The events related to the SKU.</param>
        /// <returns>A list of orders that were previously allocated.</returns>
        public List<CustomerOrder> GetAffectedOrdersFromEvents(List<CustomerOrder> orders, List<DomainEvent> events)
        {
            int i = 0;

            List<CustomerOrder> customerOrder = new List<CustomerOrder>();
            foreach (var allocationEvent in events)
            {   
               
             
               
                if (allocationEvent is SkuQuantityAllocated skuQuantityAllocated)
                {
                    string consume = orders[i].GetId();
                    string orderid = skuQuantityAllocated.OrderId;
                    i += 1;
                    var order = orders
                                .FirstOrDefault(o =>
                                    o.GetId() == skuQuantityAllocated.OrderId 
                                   
                                );

                    if (order != null)
                    {
                        order.GetOrderLines().RemoveAll(l => l.GetOrderId() != skuQuantityAllocated.LineNumber);
                      
                        customerOrder.Add(order);
                       
                    }


                }


            }
            return customerOrder;
        }
       **/

         /// <param name="orders">All existing orders.</param>
        /// <param name="events">The events related to the SKU.</param>
        /// <returns>A list of orders that were previously allocated.</returns>
        public virtual List<CustomerOrder> GetAffectedOrdersFromEvents(List<CustomerOrder> orders, List<DomainEvent> events)
        {
            // Regroupe les lignes concernées par commande
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
                        var clonedOrder = new CustomerOrder(order.GetId(),order.OrderDate, order.IsCompleteDeliveryRequired, order.Priority,order.GetOrderStatus());
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
        /// Attempts to allocate stock to a list of orders.
        /// </summary>
        /// <param name="sortedCustomerOrders">A sorted list of customer orders.</param>
        public virtual async Task TryAllocateWithFallbackAsync(List<CustomerOrder> sortCustomerOrders)
        {

            foreach (var order in sortCustomerOrders)
            {
                await _orderAllocationService.AllocateStockToOrderAsync(order);
            }

        }


    }
}
