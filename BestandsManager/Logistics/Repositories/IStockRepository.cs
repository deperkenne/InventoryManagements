using BestandsManager.Logistics.Model;

namespace BestandsManager.Logistics.Repository
{
    public interface IStockRepository
    {


        Task<List<Sku>> GetByProductName(string productName);



        Task<List<Sku>> GetAvailableSkusAsync(string productName);

        Task UpdateQuantityAsync(string skuid, int quatity);

        Task RollBackQuantityAsync(string skuid, int quantity);

        Task<int> GetAvailableQuantityAsync(string productName);
        Task<Sku> GetSkuByIdAsync(string skuId);


        Task ManuallyUpdate(string sku, int quantity);


    }
}
