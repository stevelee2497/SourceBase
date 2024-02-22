namespace API
{
    public class TodoItem
    {
        public Guid Id { get; set; }

        public DateOnly Date { get; set; }

        public string? Title { get; set; }
    }
}
