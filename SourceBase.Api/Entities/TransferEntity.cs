namespace SourceBase.Api.Entities;

public class TransferEntity : BaseAuditableEntity
{
    public required Guid FromWalletId { get; set; }

    public WalletEntity? FromWallet { get; set; }

    public required Guid ToWalletId { get; set; }

    public WalletEntity? ToWallet { get; set; }

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public required Guid FromTransactionId { get; set; }

    public required Guid ToTransactionId { get; set; }

    public required Guid UserId { get; set; }
}
