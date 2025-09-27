using Domain.Entities;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;

namespace Infrastructure.Interceptors;

public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : ISaveChangesInterceptor
{
    private string? GetCurrentUser() => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

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
                        entity.CreatedBy = entity.UpdatedBy = GetCurrentUser();
                        break;

                    case EntityState.Modified:
                        entity.UpdatedOn = DateTime.UtcNow;
                        entity.UpdatedBy = GetCurrentUser();
                        break;
                }
            }
        }
        return ValueTask.FromResult(result);
    }
}
