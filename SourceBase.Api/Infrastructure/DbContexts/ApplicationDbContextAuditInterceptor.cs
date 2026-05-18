using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContextAuditInterceptor(ICurrentUser currentUser) : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: IAuditableEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = currentUser.UserEmail;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = currentUser.UserEmail;
                        break;
                }
            }
        }
        return ValueTask.FromResult(result);
    }
}
