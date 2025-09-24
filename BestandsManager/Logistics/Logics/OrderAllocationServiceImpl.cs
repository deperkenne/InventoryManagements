using BestandsManager.Logistics.Execptions;
using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Logic
{
    public class OrderAllocationServiceImpl: IOrderAllocationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly OrderCancellationService _orderCancellationService;


        public OrderAllocationServiceImpl(
            IOrderRepository orderRepository,
            IStockRepository stockRepository,
            IEventStoreRepository eventStoreRepository,
            OrderCancellationService orderCancellationService)
        {
            _orderRepository = orderRepository;
            _stockRepository = stockRepository;
            _eventStoreRepository = eventStoreRepository;
            _orderCancellationService = orderCancellationService;
        }

        public OrderAllocationServiceImpl() { }


        public async Task<List<DomainEvent>> GetAllEventAsync()
        {
            return await _eventStoreRepository.GetAllEventsAsync();
        }

        /// <summary>
        /// Retrieves all orders from the repository.
        /// </summary>
        public async Task<List<CustomerOrder>> GetAllOrders()
        {
            return await _orderRepository.GetAllAsListAsync();
        }


        /// <summary>
        /// Retrieves and sorts new orders by priority and date.
        /// </summary>
        public async Task<List<CustomerOrder>> GetNewOrdersAsync(EnumOrderStatus enumOrderStatus)
        {

            var OrdersTask = _orderRepository.GetByStatusAsync(enumOrderStatus); // fectch all orders from repository with status "New"
            var newOrders = await OrdersTask; // Wait for the task to complete

            return SortOrdersByPriorityAndDate(newOrders);

        }


        /// <summary>
        /// Sorts a list of customer orders by priority and then by order date.
        /// </summary>
        public virtual List<CustomerOrder> SortOrdersByPriorityAndDate(List<CustomerOrder> customerOrder)
        {
            return
                (customerOrder)
                .OrderByDescending(o => o.Priority)
                .ThenBy(o => o.OrderDate)
                .ToList();
        }


        /// <summary>
        /// Processes and allocates stock for all new orders.
        /// </summary>
        public async Task ProcessMultipleOrdersForAllocationAsync()
        {

            var ordertasks = _orderRepository.GetNewOrdersAsync(EnumOrderStatus.NEW);
            foreach (var order in await ordertasks)
            {

                // This approach processes orders sequentially. If parallel processing is needed,
                // consider using Task.WhenAll with a list of tasks.
                await AllocateStockToOrderAsync(order);

            }

        }



        /// <summary>
        /// Attempts to allocate stock to a single order.
        /// </summary>
        public  async Task AllocateStockToOrderAsync(CustomerOrder order)
        {
            int i = -1;
            var pendingAllocations = new List<(Sku sku, int quantity)>();
            var lineNumbers = new List<string>();

            if (await TryCreateAllocationsAsync(order, pendingAllocations, lineNumbers))
            {

                foreach (var (sku, quantity) in pendingAllocations)
                {
                    i = i + 1;
                    await _eventStoreRepository.SaveEventAsync(new SkuQuantityAllocated(sku.SkuId, quantity, order.GetId(), "allocation", lineNumbers[i]));
                }

                await ApplyAllocationsAsync(pendingAllocations);
                await ApplyCustomerOderStatus(order);

                return;

            }

            Console.WriteLine(order.OrderDate);
            await _orderCancellationService.CancelOrder(order.GetId());
            return;

        }



        /// <summary>
        /// Attempts to create pending allocations for an order.
        /// </summary>
        /// <returns>True if all line items can be allocated, false otherwise (for complete delivery).</returns>
        public async Task<bool> TryCreateAllocationsAsync(CustomerOrder order, List<(Sku sku, int quantity)> pendingAllocations, List<string> lineNumbers)
        {
            foreach (var lineItem in order.GetOrderLines())
            {
                var remainingQuantity = lineItem.GetQuantityRequested();

                var availableSkus = await _stockRepository.GetAvailableSkusAsync(lineItem.GetProductName());

                if (availableSkus == null && order.IsCompleteDeliveryRequired) throw new CompleteDeliverySkuNotFoundException($"No SKUs available for product '{lineItem.GetProductName()}', and complete delivery is required.");

                if (availableSkus == null && !order.IsCompleteDeliveryRequired) continue;

                foreach (var sku in availableSkus)
                {
                    if (sku.GetIsLocked()) continue;

                    if (remainingQuantity <= 0) break;

                    int allocatableQuantity = Math.Min(sku.GetAllocatedQuantity(), remainingQuantity);

                    if (allocatableQuantity > 0)
                    {
                        pendingAllocations.Add((sku, allocatableQuantity));

                        lineNumbers.Add(lineItem.GetOrderId());

                        remainingQuantity -= allocatableQuantity;
                    }
                }

                // If complete delivery is required and we couldn't fulfill the line item, rollback
                if (remainingQuantity > 0 && order.IsCompleteDeliveryRequired) return false;

            }
            return true;
        }


        /// <summary>
        /// Updates the quantity of SKUs in the stock repository.
        /// </summary>
        public async Task ApplyAllocationsAsync(List<(Sku sku, int quantity)> allocations)
        {
            foreach (var (sku, quantity) in allocations)
            {
                await _stockRepository.UpdateQuantityAsync(sku.SkuId, quantity);
            }
        }


        /// <summary>
        ///  updates the order status.
        /// </summary>
        public async Task ApplyCustomerOderStatus(CustomerOrder order)
        {
            await _orderRepository.UpdateStatusAsync(order);
        }


        /**
        public async Task ProcessMultipleOrdersForAllocationAsync()
        {
            // var allocationTasks = new List<Task>();
            //var ordertasks = orderRepository.GetAllAsListAsync();
            var ordertasks = GetNewOrdersAsync(EnumOrderStatus.NEW);
            foreach (var order in await ordertasks)
            {
                if (order != null)
                {
                    await AllocateStockToOrderAsync(order);

                }

            }


            //await Task.WhenAll(allocationTasks);
        }

        **/






        /**
        public virtual async Task AllocateStockToOrderAsync1(CustomerOrder order)
        {

            int i = -1;
            List<(Sku sku, int quantity)> pendingAllocations = new List<(Sku, int)>();
            List<string> lineNumbers = new List<string>();

            bool isAllocationPossible = true;

            foreach (var lineItem in order.GetOrderLines())
            {

                string productName = lineItem.GetProductName();
                int remainingQuantity = lineItem.GetQuantityRequested();



                var availableSkus = await _stockRepository.GetAvailableSkusAsync(productName); // orderline sku

                if (availableSkus == null || !availableSkus.Any())
                {

                    throw new ProductNotFoundException($"No SKUs found for product: {productName}");

                }

                foreach (var sku in availableSkus)
                {
                    if (sku.isLocked)
                    {
                        continue;
                    }

                    if (remainingQuantity <= 0)
                        break;
                    int allocatableQuantity = Math.Min(sku.GetAllocatedQuantity(), remainingQuantity);
                    if (allocatableQuantity > 0)
                    {
                        pendingAllocations.Add((sku, allocatableQuantity));
                        lineNumbers.Add(lineItem.GetOrderId());

                        remainingQuantity -= allocatableQuantity;
                    }
                }



                // If complete delivery is required and we couldn't fulfill the line item, rollback
                if (remainingQuantity > 0)
                {

                    if (order.IsCompleteDeliveryRequired())
                    {

                        isAllocationPossible = false;
                        Console.WriteLine(lineItem.GetProductName());

                        break;
                    }

                }


            }
            // This could be a single transaction in a real database.
            // await ApplyAllocationsAsync(pendingAllocations); gut for anulation


            if (isAllocationPossible)
            {
                foreach (var (sku, quantity) in pendingAllocations)
                {
                    i = i + 1;
                    var skuQuantityAllocated = new SkuQuantityAllocated(sku.SkuId, quantity, order.GetId(), "allocation", lineNumbers[i]);
                    await _eventStoreRepository.SaveEventAsync(skuQuantityAllocated);
                }
                await ApplyCustomerOderStatus(order);
                await ApplyAllocationsAsync(pendingAllocations);
            }


            else
            {
                Console.WriteLine(order.GetOrderDate());
                await _orderCancellationService.CancelOrder(order.GetId());

            }

        }
        **/





    }


}
