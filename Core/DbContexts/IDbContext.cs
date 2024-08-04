using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.DbContexts
{
    public interface IDbContext
    {
        DbSet<UserEntity> Users { get; set; }
        DbSet<TodoItemEntity> TodoItems { get; set; }
        DbSet<AuditHistoryEntity> AuditHistories { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        string CurrentUserId { get; }
    }
}
