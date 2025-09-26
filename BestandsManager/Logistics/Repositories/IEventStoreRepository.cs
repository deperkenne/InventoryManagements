using InventoryManagement.Logistics.Models;

namespace InventoryManagement.Logistics.Repositories
{
    public interface IEventStoreRepository
    {
        Task SaveEventAsync(DomainEvent domainEvent);
        Task<List<DomainEvent>> GetAllEventsAsync();
        Task<List<DomainEvent>> GetEventsForOrderAsync(string orderId);
        Task<List<DomainEvent>> GetEventsForSkuAsync(string skuId);
    }
}
