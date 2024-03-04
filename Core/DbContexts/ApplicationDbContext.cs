using Core.Entities;
using Core.Helpers;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Core.DbContexts
{
    public class ApplicationDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>
    {
        private readonly ISessionUserHelper _sessionUserHelper;

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ISessionUserHelper sessionUserHelper) : base(options)
        {
            _sessionUserHelper = sessionUserHelper;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
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
                    Author = _sessionUserHelper.User,
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString(),
                    Current = JsonSerializer.Serialize(entry.CurrentValues.ToObject())
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = _sessionUserHelper.User;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = _sessionUserHelper.User;
                        auditHistory.Original = JsonSerializer.Serialize(entry.OriginalValues.ToObject());
                        auditHistory.Changes = JsonSerializer.Serialize(entry.Properties.Where(prop => prop.IsModified).Select(prop => new
                        {
                            Property = prop.Metadata.PropertyInfo.Name,
                            Value = prop.CurrentValue,
                            Original = prop.OriginalValue,
                        }));
                        break;
                }

                auditHistories.Add(auditHistory);
            }

            AuditHistories.AddRange(auditHistories);
        }
    }
}
