using BestandsManager.Event;
using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Repositories.Impl
{
    // USE THIS CLASS ONLY FOR INTEGRATION TESTING!
    public class InMemoryEventStoreImpl : IEventStoreRepository
    {
        private List<DomainEvent> _events = new List<DomainEvent>();// db simulation

        public async Task SaveEventAsync(DomainEvent domainEvent)
        {
            _events.Add(domainEvent);
            await Task.CompletedTask;
        }
        public Task<List<DomainEvent>> GetEventsForOrderAsync(string orderId)
        {
            var relatedEvents = _events.Where(e =>
                    e is SkuQuantityAllocated allocation && allocation.OrderId == orderId ||
                    e is OrderCancelled cancellation && cancellation.OrderId == orderId
            ).ToList();

            return Task.FromResult(relatedEvents);
        }
        Task<List<DomainEvent>> IEventStoreRepository.GetAllEventsAsync()
        {
            return Task.FromResult(_events.ToList());
        }
        public Task<List<DomainEvent>> GetEventsForSkuAsync(string skuId)
        {
            var relatedEvents = _events.Where(e =>
                   e is SkuQuantityAllocated allocation && allocation.SkuId == skuId).ToList();

            return Task.FromResult(relatedEvents.ToList());
        }
    }
}
