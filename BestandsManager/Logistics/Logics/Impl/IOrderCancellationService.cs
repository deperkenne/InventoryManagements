using BestandsManager.Logistics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestandsManager.Logistics.Logics.Impl
{
    public interface IOrderCancellationService
    {
        Task CancelOrderAsync(string orderId);
        Task CreateAndSaveCancellationEventAsync(List<DomainEvent> eventsToRevert);
    }
}
