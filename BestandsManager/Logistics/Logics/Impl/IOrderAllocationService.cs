using InventoryManagement.Logistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Logistics.Logics.Impl
{
     public interface IOrderAllocationService
    {
        Task ProcessMultipleOrdersForAllocationAsync();
        Task AllocateStockToOrderAsync(CustomerOrder order);
        Task<bool> TryCreateAllocationsAsync(CustomerOrder order, int remainingQuantity, List<(Sku sku, int quantity,string line)> pendingAllocations);
    }
}
