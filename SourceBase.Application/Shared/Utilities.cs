using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SourceBase.Application.Shared;

public static class Utilities
{
    extension<TEntity>(IQueryable<TEntity> query)
    {
        public async Task<PagingResponse<TResponse>> PaginateAsync<TResponse>(Expression<Func<TEntity, TResponse>> selector, PagingRequest paging, CancellationToken ct)
        {
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(paging.Direction, paging.Order).Skip(((paging.Page ?? 1) - 1) * (paging.Limit ?? 10)).Take(paging.Limit ?? 10).Select(selector).ToListAsync(ct);
            return new PagingResponse<TResponse>(items, paging.Page ?? 1, paging.Limit ?? 10, total);
        }

        public IQueryable<TEntity> OrderBy(string? direction, PagingOrder? order = PagingOrder.Asc)
        {
            if (direction is null) return query;
            var property = typeof(TEntity).GetProperty(direction) ?? throw new ArgumentException($"Invalid sorting column: {direction}");
            var parameter = Expression.Parameter(typeof(TEntity));
            var keySelector = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(Expression.Property(parameter, property), typeof(object)), parameter);
            return order == PagingOrder.Asc ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
    }

    extension(IEnumerable<string> self)
    {
        public string[] Normalize()
        {
            return [.. self.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }

    extension(object self)
    {
        public string Serialize()
        {
            return JsonSerializer.Serialize(self, JsonOptions);
        }
    }

    extension(string self)
    {
        public T Deserialize<T>()
        {
            return JsonSerializer.Deserialize<T>(self, JsonOptions) ?? throw new JsonException("Deserialization resulted in null");
        }

        public string WithId(Guid id)
        {
            return self.Replace("{id:guid}", id.ToString(), StringComparison.Ordinal);
        }

        public string WithCode(string code)
        {
            return self.Replace("{code}", code, StringComparison.Ordinal);
        }

        public string WithState(string state)
        {
            return self.Replace("{state}", state, StringComparison.Ordinal);
        }
    }

    extension(ClaimsPrincipal claimsPrincipal)
    {
        public Guid? UserId => Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

        public string? Email => claimsPrincipal.FindFirstValue(ClaimTypes.Email);

        public string? UserName => claimsPrincipal.FindFirstValue(ClaimTypes.Name);

        public string? SecurityStamp => claimsPrincipal.FindFirstValue(Constants.SecurityStampClaimType);

        public IEnumerable<string> Roles => claimsPrincipal.FindAll(ClaimTypes.Role).Select(x => x.Value);
    }

    public static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new TrimmingJsonConverter() }
    };

    extension(HttpContext context)
    {
        public string? GetClientIp()
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (IPAddress.TryParse(firstIp, out _))
                    return firstIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
