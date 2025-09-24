using BestandsManager.Logistics.Model;


namespace BestandsManager.Logistics.Repository
{
    public interface IOrderRepository
    {

        Task AddCustomerOrder(CustomerOrder customerOrder);

        /// <summary>
        /// Saves a new customer order to the repository.
        /// </summary>
        /// <param name="order">The customer order to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous save operation. The task result is the saved <see cref="CustomerOrder"/>.</returns>
        Task<CustomerOrder> SaveAsync(CustomerOrder order);

        /// <summary>
        /// Retrieves a customer order by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the order.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous get operation. The task result is the found <see cref="CustomerOrder"/>, or <c>null</c> if no order is found.</returns>
        Task<CustomerOrder?> GetByIdAsync(string id);


        // <summary>
        /// Retrieves all customer orders from the repository as a list.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous get operation. The task result is a list of all <see cref="CustomerOrder"/> objects.</returns>
        Task<List<CustomerOrder>> GetAllAsListAsync();


        /// <summary>
        /// Updates the status of an existing customer order.
        /// </summary>
        /// <param name="customerOrder">The customer order with the updated status.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous update operation.</returns>
        Task UpdateStatusAsync(CustomerOrder customerOrder);


        /// <summary>
        /// Retrieves a list of customer orders that have a specific status.
        /// </summary>
        /// <param name="enumOrderStatus">The status to filter orders by.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous get operation. The task result is a list of <see cref="CustomerOrder"/> objects with the specified status.</returns>
        Task<List<CustomerOrder>> GetByStatusAsync(EnumOrderStatus enumOrderStatus);


        Task<List<CustomerOrder>> SortOrdersByPriorityAndDateAsync(List<CustomerOrder>customeOrderSorted);

        Task<List<CustomerOrder>> GetNewOrdersAsync(EnumOrderStatus enumOrderStatus);

    }
}
