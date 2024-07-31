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

        public string? Current {  get; set; }

        public string? Original { get; set; }

        public string? Changes { get; set; }
    }
}
