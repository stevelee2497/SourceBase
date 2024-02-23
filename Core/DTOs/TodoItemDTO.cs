namespace Core.DTOs
{
    internal class TodoItemDTO
    {
        public Guid Id { get; set; }

        public DateOnly Date { get; set; }

        public string? Title { get; set; }
    }
}
