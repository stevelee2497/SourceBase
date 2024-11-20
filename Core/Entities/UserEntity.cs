using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public override string? UserName { get; set; }

    public override string? Email { get; set; }

    public override string? PhoneNumber { get; set; }

    public List<RoleEntity> Roles { get; set; } = [];

    [Column(Order = 96)]
    public DateTime? CreatedOn { get; set; }

    [Column(Order = 97)]
    public string? CreatedBy { get; set; }

    [Column(Order = 98)]
    public DateTime? UpdatedOn { get; set; }

    [Column(Order = 99)]
    public string? UpdatedBy { get; set; }
}