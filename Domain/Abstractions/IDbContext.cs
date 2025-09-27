using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Domain.Abstractions;

public interface IDbContext
{
    DbSet<UserEntity> Users { get; set; }
    DbSet<RoleEntity> Roles { get; set; }
    DbSet<ProfileEntity> Profiles { get; set; }
    DbSet<TodoItemEntity> TodoItems { get; set; }
    DbSet<AuditHistoryEntity> AuditHistories { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}