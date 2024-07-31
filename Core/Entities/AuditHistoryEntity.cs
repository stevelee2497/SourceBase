namespace Core.Entities
{
    public class AuditHistoryEntity
    {
        public Guid Id { get; set; }

        public required string Author { get; set; }

        public required string Action { get; set; }

        public DateTime ActionOn { get; set; }

        public required string EntityType { get; set; }

        public required string EntityId { get; set; }

        public required string Current {  get; set; }

        public required string Original { get; set; }

        public required string Changes { get; set; }
    }
}
