using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

public class TransactionEntity : BaseAuditableEntity
{
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public required Guid WalletId { get; set; }

    public WalletEntity? Wallet { get; set; }

    public Guid? CategoryId { get; set; }

    public CategoryEntity? Category { get; set; }

    public required Guid UserId { get; set; }

    public bool IsTransfer { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    Income,
    Expense
}
