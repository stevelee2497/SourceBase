using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Domain.Entities;

namespace SourceBase.Api.Infrastructure.Interfaces;

public interface IDbContext
{
    DbSet<ApplicationUser> Users { get; }

    DbSet<ApplicationRole> Roles { get; }

    DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    DbSet<TodoItemEntity> TodoItems { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}