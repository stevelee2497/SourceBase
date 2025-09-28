using Microsoft.EntityFrameworkCore;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Abstractions;

public interface IDbContext
{
    DbSet<UserEntity> Users { get; set; }
    DbSet<RoleEntity> Roles { get; set; }
    DbSet<TodoItemEntity> TodoItems { get; set; }
    DbSet<AuditHistoryEntity> AuditHistories { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}