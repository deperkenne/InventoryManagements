using BestandsManager.Logistics.Execptions;
using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class OrderAllocationServiceImpl : IOrderAllocationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IOrderCancellationService _orderCancellationService;
        public List<(Sku sku, int quantity, string line)> pendingAllocations = new List<(Sku, int, string)>();

        public OrderAllocationServiceImpl(
            IOrderRepository orderRepository,
            IStockRepository stockRepository,
            IEventStoreRepository eventStoreRepository,
            IOrderCancellationService orderCancellationService)
        {
            _orderRepository = orderRepository;
            _stockRepository = stockRepository;
            _eventStoreRepository = eventStoreRepository;
            _orderCancellationService = orderCancellationService;
        }

        /// <summary>
        /// Processes and allocates stock for all new orders.
        /// </summary>
        public async Task ProcessMultipleOrdersForAllocationAsync()
        {
            var ordertasks = _orderRepository.GetNewOrdersAsync(EnumOrderStatus.NEW);

            // transaction start
            foreach (var order in await ordertasks)
            {
                await AllocateStockToOrderAsync(order);
            }
        }

        /// <summary>
        /// Attempts to allocate stock for all line items within a customer order.
        /// Aggregates all allocations before attempting a single transaction.
        /// </summary>
        /// <param name="order">The customer order to allocate stock for.</param>
        public async Task AllocateStockToOrderAsync(CustomerOrder order)
        {
            bool isAllocationPossible = true;
            List<string> lineNumbers = new List<string>();

            foreach (var lineItem in order.GetOrderLines())
            {
                    pendingAllocations.Clear();
                    string productName = lineItem.GetProductName();
                    int remainingQuantity = lineItem.GetQuantityRequested();
                    var availableSkus = await _stockRepository.GetAvailableSkusAsync(lineItem.GetProductName());

                    if (availableSkus == null && order.IsCompleteDeliveryRequired) throw new CompleteDeliverySkuNotFoundException($"No SKUs available for product '{lineItem.GetProductName()}', and complete delivery is required.");

                    else if (availableSkus == null && !order.IsCompleteDeliveryRequired) continue;

                    foreach (var sku in availableSkus)
                    {
                        if (sku.GetIsLocked()) continue;

                        else if (remainingQuantity <= 0) break;

                        int allocatableQuantity = Math.Min(sku.GetAllocatedQuantity(), remainingQuantity);

                        if (allocatableQuantity > 0)
                        {
                            pendingAllocations.Add((sku, allocatableQuantity, lineItem.GetOrderId()));
                            remainingQuantity -= allocatableQuantity;
                        }
                    }

                    if (!await TryCreateAllocationsAsync(order, remainingQuantity, pendingAllocations))
                    {
                        isAllocationPossible = false;
                    }
            }

            if (isAllocationPossible)
            {
                Console.WriteLine("transaction commited");
            }
        }

        /// <summary>
        /// Attempts to finalize an allocation by saving events and updating the stock and order status.
        /// Triggers order cancellation if the allocation fails or if complete delivery is impossible.
        /// </summary>
        public async Task<bool> TryCreateAllocationsAsync(CustomerOrder order, int remainingQuantity, List<(Sku sku, int quantity, string line)> pendingAllocations)
        {
            try 
            {
                    // If complete delivery is required and we couldn't fulfill the line item, rollback
                    if (remainingQuantity > 0 && order.IsCompleteDeliveryRequired)
                    {
                        throw new CompleteDeliveryException($"complete delivery is required , rollback the transaction.");              
                    }

                    else
                    {
                        foreach (var (sku, quantity, line) in pendingAllocations)
                        {
                            await _eventStoreRepository.SaveEventAsync(new SkuQuantityAllocated(sku.SkuId, quantity, order.GetId(), "allocation", line));
                        }

                        await _stockRepository.ApplyAllocationsAsync(pendingAllocations); // transaction Operation
                        await _orderRepository.UpdateStatusAsync(order);

                        return true;
                    }
            }
            catch (Exception ex)
            {
                    await _orderCancellationService.CancelOrderAsync(order.GetId()); // transaction Rollback
                    Console.WriteLine($"Error during allocation: {ex.Message}");

                    return false;
            }
        }
    }
}
