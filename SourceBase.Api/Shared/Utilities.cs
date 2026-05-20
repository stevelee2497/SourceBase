using System.Linq.Expressions;
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

    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string? direction, PagingOrder? order = PagingOrder.Asc)
    {
        if (direction is null) return query;
        var property = typeof(T).GetProperty(direction) ?? throw new ArgumentException($"Invalid sorting column: {direction}");
        var param = Expression.Parameter(typeof(T));
        var keySelector = Expression.Lambda<Func<T, object>>(Expression.Convert(Expression.Property(param, property), typeof(object)), param);
        return order == PagingOrder.Asc ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PagingOrder
{
    Asc,
    Desc
}

public record PagingRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, string? Direction = null);

public record PagingResponse<T>(List<T> Items, int Page, int Limit, int Total);