namespace InventoryManagement.Logistics.Models
{
    public abstract class DomainEvent
    {

        /// <summary>
        /// Gets the timestamp when the event was created in UTC.
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
