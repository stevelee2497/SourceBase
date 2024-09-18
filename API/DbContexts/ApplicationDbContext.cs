using Core.DbContexts;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.DbContexts
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
    {
        #region DbSets

        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        #endregion

        #region Ctors

        public string CurrentUserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous user";

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddAuditLog();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddAuditLog();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AddAuditLog()
        {
            var auditHistories = new List<AuditHistoryEntity>();
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry is not { Entity: IBaseEntity entity })
                    continue;

                var auditHistory = new AuditHistoryEntity
                {
                    Action = entry.State.ToString(),
                    ActionOn = DateTime.UtcNow,
                    Author = CurrentUserId,
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString()
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = CurrentUserId;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = CurrentUserId;
                        auditHistory.Original = JsonSerializer.Serialize(entry.OriginalValues.ToObject());
                        auditHistory.Changes = JsonSerializer.Serialize(entry.Properties.Where(prop => prop.IsModified).Select(prop => new
                        {
                            Property = prop.Metadata.PropertyInfo?.Name,
                            Current = prop.CurrentValue,
                            Original = prop.OriginalValue,
                        }));
                        break;
                }

                auditHistory.Current = JsonSerializer.Serialize(entry.CurrentValues.ToObject());
                auditHistories.Add(auditHistory);
            }

            AuditHistories.AddRange(auditHistories);
        }

        #endregion
    }
}
