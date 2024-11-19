using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class RoleEntity : IdentityRole<Guid>, IBaseEntity
    {
        public override string? Name { get; set; }

        public List<UserEntity> Users { get; set; } = [];

        [Column(Order = 96)]
        public DateTime? CreatedOn { get; set; }

        [Column(Order = 97)]
        public Guid? CreatedBy { get; set; }

        [Column(Order = 98)]
        public DateTime? UpdatedOn { get; set; }

        [Column(Order = 99)]
        public Guid? UpdatedBy { get; set; }
    }
}
