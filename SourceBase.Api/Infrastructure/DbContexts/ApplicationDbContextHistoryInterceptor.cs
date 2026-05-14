using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.Identity;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContextHistoryInterceptor(CurrentUser currentUser) : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            var auditHistories = new List<AuditHistoryEntity>();
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: IBaseEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                auditHistories.Add(new AuditHistoryEntity
                {
                    Action = entry.State.ToString(),
                    ActionOn = DateTime.UtcNow,
                    Author = currentUser.UserEmail,
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString(),
                    Current = JsonSerializer.Serialize(entry.CurrentValues.ToObject()),
                    Original = JsonSerializer.Serialize(entry.OriginalValues.ToObject()),
                    Changes = JsonSerializer.Serialize(entry.Properties.Where(prop => prop.IsModified).Select(prop => new
                    {
                        Property = prop.Metadata.PropertyInfo?.Name,
                        Current = prop.CurrentValue,
                        Original = prop.OriginalValue,
                    }))
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
