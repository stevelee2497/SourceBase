using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public interface IBaseEntity
    {
        string CreatedBy { get; set; }
        DateTime CreatedOn { get; set; }
        Guid Id { get; set; }
        string UpdatedBy { get; set; }
        DateTime UpdatedOn { get; set; }
    }

    public abstract class BaseEntity : IBaseEntity
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
    }
}
