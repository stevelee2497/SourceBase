using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Core.DbContexts
{
    public abstract class IdentityAuditDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>
    {
        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public IdentityAuditDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

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

        public abstract string GetAuthor();

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
                    Author = GetAuthor(),
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString()
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = GetAuthor();
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = GetAuthor();
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
    }
}
