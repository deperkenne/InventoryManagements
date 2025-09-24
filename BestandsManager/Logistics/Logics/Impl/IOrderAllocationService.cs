using BestandsManager.Logistics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestandsManager.Logistics.Logics.Impl
{
     public interface IOrderAllocationService
    {

        Task ProcessMultipleOrdersForAllocationAsync();

        Task AllocateStockToOrderAsync(CustomerOrder order);

        Task ApplyAllocationsAsync(List<(Sku sku, int quantity)> allocations);

        Task ApplyCustomerOderStatus(CustomerOrder order);

        Task<bool> TryCreateAllocationsAsync(CustomerOrder order, List<(Sku sku, int quantity)> pendingAllocations, List<string> lineNumbers);
    }
}
