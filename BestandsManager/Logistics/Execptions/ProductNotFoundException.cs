namespace BestandsManager.Logistics.Execptions
{
    internal class ProductNotFoundException : Exception
    {

        public ProductNotFoundException(string message) : base(message)
        {
        }
    }
}
