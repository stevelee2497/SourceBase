using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class TodoItemEntity : BaseEntity
    {
        public string Title { get; set; }

        public DateOnly Date { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public ItemStatus Status { get; set; }
    }

    public enum ItemStatus
    {
        Open,
        Completed,
        Archived
    }
}
