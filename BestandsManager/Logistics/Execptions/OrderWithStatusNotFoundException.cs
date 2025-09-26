namespace InventoryManagement.Logistics.Execptions
{
    internal class OrderWithStatusNotFoundException : Exception
    {
        public OrderWithStatusNotFoundException(string message) : base(message)
        {
        }
    }
}
