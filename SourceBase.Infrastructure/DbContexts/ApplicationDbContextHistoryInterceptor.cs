using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Domain.Entities;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.DbContexts;

public class ApplicationDbContextHistoryInterceptor(ICurrentUser currentUser, IDateTime dateTime) : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            var auditHistories = new List<AuditHistoryEntity>();
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: BaseAuditableEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                auditHistories.Add(new AuditHistoryEntity
                {
                    Action = entry.State.ToString(),
                    ActionOn = dateTime.UtcNow,
                    Author = currentUser.UserName,
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString(),
                    Current = entry.CurrentValues.ToObject().Serialize(),
                    Original = entry.OriginalValues.ToObject().Serialize(),
                    Changes = entry.Properties.Where(prop => prop.IsModified).Select(prop => new
                    {
                        property = prop.Metadata.PropertyInfo?.Name,
                        current = prop.CurrentValue,
                        original = prop.OriginalValue,
                    }).Serialize()
                });
            }

            if (auditHistories.Any())
            {
                dbContext.AuditHistories.AddRange(auditHistories);
            }
        }
        return ValueTask.FromResult(result);
    }
}
