using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Application.Abstractions;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.DbContexts;

public class ApplicationDbContextAuditInterceptor(IUserContext userContext) : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: BaseEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = userContext.UserEmail;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = userContext.UserEmail;
                        break;
                }
            }
        }
        return ValueTask.FromResult(result);
    }
}
