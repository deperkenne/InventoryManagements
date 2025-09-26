
using InventoryManagement.Logistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Logistics.Logics.Impl
{
    public interface IAdjustSkuQuantityManuellyService
    {
        Task AdjustSkuQuantityManuallyAsync(string skuId, int newQuantity);
        Task HandleStockUpdateEvent(string skuId);
        List<CustomerOrder> GetAffectedOrdersFromEvents(List<CustomerOrder> orders, List<DomainEvent> events);
        Task TryAllocateWithFallbackAsync(List<CustomerOrder> sortedOrders);
    }
}
