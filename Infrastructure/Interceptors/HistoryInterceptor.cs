using Domain.Entities;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Infrastructure.Interceptors;

public class HistoryInterceptor(IHttpContextAccessor httpContextAccessor) : ISaveChangesInterceptor
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

                auditHistories.Add(new AuditHistoryEntity
                {
                    Action = entry.State.ToString(),
                    ActionOn = DateTime.UtcNow,
                    Author = GetCurrentUser(),
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
