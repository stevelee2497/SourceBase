namespace Core.Entities
{
    public class TodoItemEntity : BaseEntity
    {
        public string Title { get; set; }

        public DateOnly Date { get; set; }
    }
}
