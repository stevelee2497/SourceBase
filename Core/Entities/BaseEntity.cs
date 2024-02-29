using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        [Column(Order = 96)]
        public DateTime CreatedOn { get; set; }

        [Column(Order = 97)]
        public string CreatedBy { get; set; }

        [Column(Order = 98)]
        public DateTime UpdatedOn { get; set; }

        [Column(Order = 99)]
        public string UpdatedBy { get; set; }

        [Column(Order = 100)]
        public bool IsDeleted { get; set; }
    }
}
