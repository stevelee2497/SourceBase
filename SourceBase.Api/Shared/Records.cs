
using System.Text.Json.Serialization;

namespace SourceBase.Api.Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PagingOrder
{
    Asc,
    Desc
}

public record PagingRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, string? Direction = null);

public record PagingResponse<T>(List<T> Items, int Page, int Limit, int Total);