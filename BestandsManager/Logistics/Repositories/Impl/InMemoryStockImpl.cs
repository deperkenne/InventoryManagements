using BestandsManager.Logistics.Model;
using BestandsManager.Logistics.Repository;

namespace BestandsManager.Logistics.Repositories.Impl
{
    public class InMemoryStockImpl : IStockRepository
    {

        public List<Sku> skuInMemory = new List<Sku>();
      

        public InMemoryStockImpl()
        {
            InitializeWarehouse();

        }

        public void InitializeWarehouse()
        {

            Sku sku_01 = new Sku("skuId_01", "COLA_L1", "Shelf 1", false, 100);
            Sku sku_02 = new Sku("skuId_02", "COLA_L1", "Shelf 2", true, 200);
            Sku sku_03 = new Sku("skuId_03", "FANTA_L1", "Shelf 2", false, 200);
            Sku sku_04 = new Sku("skuId_04", "WATER_L1", "Shelf 3", false, 60);
            Sku sku_05 = new Sku("skuId_05", "ORANGE_L1", "Shelf 3", false, 40);
            Sku sku_06 = new Sku("skuId_06", "ORANGE_L1", "Shelf 3", true, 40);
            Sku sku_07 = new Sku("skuId_07", "ORANGE_L1", "Shelf 3", false, 40);
            Sku sku_08 = new Sku("skuId_08", "ORANGE_L1", "Shelf 4", false, 40);

            skuInMemory.Add(sku_01);
            skuInMemory.Add(sku_02);
            skuInMemory.Add(sku_03);
            skuInMemory.Add(sku_04);
            skuInMemory.Add(sku_05);
            skuInMemory.Add(sku_06);
            skuInMemory.Add(sku_07);
            skuInMemory.Add(sku_08);

        }


        public async Task<int> GetAvailableQuantityAsync(string skuId)
        {
            if (string.IsNullOrEmpty(skuId))
            {
                throw new ArgumentException("SKU ID cannot be null or empty.");
            }
            Task.Delay(2000).Wait(); // Simulate some async work

            var availableQuantity = skuInMemory.Where(s => s.SkuId == skuId && s.GetIsLocked() == false).Sum(s => s.GetAllocatedQuantity());

            return availableQuantity;
        }

        public async Task<List<Sku>> GetByProductName(string productName)
        {
            if (string.IsNullOrEmpty(productName))
            {
                throw new ArgumentException("Product name cannot be null or empty.");
            }
            var skus = skuInMemory.Where(s => s.ProductName == productName && s.GetIsLocked() == false).ToList();

            return skus;
        }

        public async Task UpdateQuantityAsync(string skuid, int quantity)
        {
            if (skuid == null || quantity < 0) throw new ArgumentNullException("SKU cannot be null. or quantity cannot be least than 0");
           
            var existingSku = await GetSkuByIdAsync(skuid);

            if (existingSku != null) existingSku.SetAllocatedQuantity(existingSku.GetAllocatedQuantity() - quantity);
          
        }

        public async Task<List<Sku>> GetAvailableSkusAsync(string productName)
        {

            if (string.IsNullOrEmpty(productName)) throw new ArgumentException("Product name cannot be null or empty.");
            
            List<Sku> availableSkus = new List<Sku>();
            // var availableSkus = skuInMemory.Where(s => s.productName == productName && s.isLocked == false).ToList();
            foreach (var sku in skuInMemory)
            {
                if (sku.ProductName == productName && sku.GetIsLocked() == false)
                {
                    availableSkus.Add(sku);
                }
            }

            return availableSkus;
        }

        public async Task<Sku> GetSkuByIdAsync(string skuId)
        {
            if (string.IsNullOrEmpty(skuId)) throw new ArgumentException("SKU ID cannot be null or empty.");

            var foundSku = skuInMemory.FirstOrDefault(s => s.SkuId == skuId);

            return foundSku;
        }

        public async Task RollBackQuantityAsync(string skuid, int quantity)
        {
            if (skuid == null || quantity < 0) throw new ArgumentNullException("SKU cannot be null. or quantity cannot be least than 0");
           
            var existingSku = await GetSkuByIdAsync(skuid);

            if (existingSku != null)
            {
                int qty = existingSku.GetAllocatedQuantity();

                existingSku.SetAllocatedQuantity(qty + quantity);

                Console.WriteLine(existingSku.GetAllocatedQuantity());

            }
        }

        public async Task ManuallyUpdate(string sku, int quantity)
        {
            var existingSku = await GetSkuByIdAsync(sku);

            existingSku.SetAllocatedQuantity(quantity);
        }
    }
}
