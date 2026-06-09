using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Domain.Entities;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.DbContexts;

public class ApplicationDbContextAuditInterceptor(ICurrentUser currentUser, IDateTime dateTime) : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: BaseAuditableEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = dateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = currentUser.UserName;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = dateTime.UtcNow;
                        entity.UpdatedBy = currentUser.UserName;
                        break;
                }
            }
        }
        return ValueTask.FromResult(result);
    }
}
