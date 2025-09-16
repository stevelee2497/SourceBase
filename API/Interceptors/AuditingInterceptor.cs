using API.Contexts;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace API.Interceptors;

public class AuditingInterceptor(IHttpContextAccessor httpContextAccessor) : ISaveChangesInterceptor
{
    private string? GetCurrentUser() => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext dbContext)
        {
            var auditHistories = new List<AuditHistoryEntity>();
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry is not { Entity: IBaseEntity entity } || new[] { EntityState.Detached, EntityState.Unchanged }.Contains(entry.State))
                    continue;

                var auditHistory = new AuditHistoryEntity
                {
                    Action = entry.State.ToString(),
                    ActionOn = DateTime.UtcNow,
                    Author = GetCurrentUser(),
                    EntityType = entity.GetType().ToString(),
                    EntityId = entity.Id.ToString()
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedOn = entity.UpdatedOn = DateTime.UtcNow;
                        entity.CreatedBy = entity.UpdatedBy = GetCurrentUser();
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = GetCurrentUser();
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

            dbContext.AuditHistories.AddRange(auditHistories);
        }
        return ValueTask.FromResult(result);
    }
}
