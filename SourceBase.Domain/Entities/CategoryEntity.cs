using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

public class CategoryEntity : BaseAuditableEntity
{
    public required string Name { get; set; }

    public CategoryType Type { get; set; }

    public string? Icon { get; set; }

    public Guid? UserId { get; set; }

    public bool IsSystem { get; set; }

    public ICollection<TransactionEntity> Transactions { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CategoryType
{
    Income,
    Expense
}
