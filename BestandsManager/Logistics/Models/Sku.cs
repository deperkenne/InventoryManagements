namespace InventoryManagement.Logistics.Models
{
    public class Sku
    {
        public string SkuId { get; }
        private int AllocatedQuantity { get; set; }
        private bool IsLocked { get; set; }
        public string ProductName { get; }
        private string LocationName { get; set; }

        public Sku(string skuId, string productName, string locationName, bool isLocked, int allocatedQuantity)
        {
            SkuId = skuId;
            ProductName = productName;
            LocationName = locationName;
            IsLocked = isLocked;
            AllocatedQuantity = allocatedQuantity; 
        }

        public int GetAllocatedQuantity()
        {
            return AllocatedQuantity;
        }   

        public void SetAllocatedQuantity(int allocatedQuantity)
        {
            AllocatedQuantity = allocatedQuantity;
        }

        public void SetLocationName(string locationName)
        {
            LocationName = locationName;
        }

        public bool GetIsLocked()
        {
            return IsLocked;
        }

        public void SetIsLocked(bool isLocked)
        {
            IsLocked = isLocked;
        }

        public string GetLocationName()
        {
            return LocationName;
        }
    }
}
