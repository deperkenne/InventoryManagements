using InventoryManagement.Logistics.Models;


namespace InventoryManagement.Logistics.Repositories
{
    public interface IOrderRepository
    {
        Task AddCustomerOrder(CustomerOrder customerOrder);
        Task<CustomerOrder> SaveAsync(CustomerOrder order);
        Task<CustomerOrder?> GetByIdAsync(string id);
        Task<List<CustomerOrder>> GetAllAsListAsync();
        Task UpdateStatusAsync(CustomerOrder customerOrder);
        Task<List<CustomerOrder>> GetByStatusAsync(EnumOrderStatus enumOrderStatus);
        Task<List<CustomerOrder>> SortOrdersByPriorityAndDateAsync(List<CustomerOrder>customeOrderToSort);
        Task<List<CustomerOrder>> GetNewOrdersAsync(EnumOrderStatus enumOrderStatus);
    }
}
