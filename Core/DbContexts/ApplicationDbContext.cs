using Core.Entities;
using Core.Helpers;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Core.DbContexts
{
    public class ApplicationDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>
    {
        private readonly ISessionUserHelper _sessionUserHelper;

        public DbSet<TodoItemEntity> TodoItems { get; set; }

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
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry is not { Entity: IBaseEntity entity })
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = _sessionUserHelper.User;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = _sessionUserHelper.User;
                        break;

                    case EntityState.Deleted:
                        // TODO: Add audit record to history table
                        break;
                }
            }
        }
    }
}
