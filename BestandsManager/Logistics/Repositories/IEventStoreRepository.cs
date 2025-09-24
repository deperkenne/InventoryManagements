using BestandsManager.Logistics.Model;

namespace BestandsManager.Logistics.Repository
{
    public interface IEventStoreRepository
    {

        /// <summary>
        /// Saves an event to the "event store."
        /// </summary>
        /// <param name="domainEvent">The event to be saved.</param>
        Task SaveEventAsync(DomainEvent domainEvent);


        /// <summary>
        /// Retrieves all events from the store.
        /// </summary>
        /// <returns>A list of all events.</returns>
        Task<List<DomainEvent>> GetAllEventsAsync();

        // <summary>
        /// Retrieves all events related to a specific order.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <returns>A list of events (allocations and cancellations) for the given order.</returns>
        Task<List<DomainEvent>> GetEventsForOrderAsync(string orderId);

        /// <summary>
        /// Retrieves all allocation events related to a specific SKU (product).
        /// </summary>
        /// <param name="skuId">The unique identifier of the SKU.</param>
        /// <returns>A list of allocation events for the given SKU.</returns>
        Task<List<DomainEvent>> GetEventsForSkuAsync(string skuId);
    }
}
