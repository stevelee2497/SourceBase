using System.Text.Json.Serialization;

namespace SourceBase.Application.Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PagingOrder
{
    Asc,
    Desc
}

public record PagingRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, string? Direction = null);

public record PagingResponse<T>(List<T> Items, int Page, int Limit, int Total);

public record EmailMessage(string To, string Subject, string Body);