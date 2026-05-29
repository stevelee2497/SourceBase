using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;

namespace SourceBase.Api.Shared.Interfaces;

public interface IDbContext
{
    DbSet<UserEntity> Users { get; }

    DbSet<RoleEntity> Roles { get; }

    DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    DbSet<TodoItemEntity> TodoItems { get; set; }

    DbSet<TodoListEntity> TodoLists { get; set; }

    DbSet<EmailEntity> Emails { get; set; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}