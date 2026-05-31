namespace SourceBase.Api.Entities;

public class WalletEntity : BaseAuditableEntity
{
    public required string Name { get; set; }

    public decimal InitialBalance { get; set; }

    public required string Currency { get; set; }

    public string? Icon { get; set; }

    public required Guid UserId { get; set; }

    public ICollection<TransactionEntity> Transactions { get; set; } = [];
}
