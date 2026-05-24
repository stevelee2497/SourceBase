using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SourceBase.Api.Shared;

public static class Utilities
{
    public static async Task<PagingResponse<TResponse>> PaginateAsync<TEntity, TResponse>(this IQueryable<TEntity> query, Expression<Func<TEntity, TResponse>> selector, PagingRequest paging, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(paging.Direction, paging.Order).Skip(((paging.Page ?? 1) - 1) * (paging.Limit ?? 10)).Take(paging.Limit ?? 10).Select(selector).ToListAsync(ct);
        return new PagingResponse<TResponse>(items, paging.Page ?? 1, paging.Limit ?? 10, total);
    }

    public static string[] Normalize(this IEnumerable<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string? direction, PagingOrder? order = PagingOrder.Asc)
    {
        if (direction is null) return query;
        var property = typeof(T).GetProperty(direction) ?? throw new ArgumentException($"Invalid sorting column: {direction}");
        var parameter = Expression.Parameter(typeof(T));
        var keySelector = Expression.Lambda<Func<T, object>>(Expression.Convert(Expression.Property(parameter, property), typeof(object)), parameter);
        return order == PagingOrder.Asc ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
    }

    public static JsonSerializerOptions JsonOptions => new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static string Serialize(this object obj)
    {
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    public static T Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException("Deserialization resulted in null");
    }
}
