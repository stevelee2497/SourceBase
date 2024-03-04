namespace Core.Entities
{
    public class AuditHistoryEntity
    {
        public Guid Id { get; set; }

        public string Author { get; set; }

        public string Action { get; set; }

        public DateTime ActionOn { get; set; }

        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public string Current {  get; set; }

        public string Original { get; set; }

        public string Changes { get; set; }
    }
}
