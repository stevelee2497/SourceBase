using Microsoft.EntityFrameworkCore;

namespace SourceBase.Application.Shared.Interfaces;

public interface IDbContext
{
    DbSet<UserEntity> Users { get; }

    DbSet<RoleEntity> Roles { get; }

    DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    DbSet<TodoItemEntity> TodoItems { get; set; }

    DbSet<TodoListEntity> TodoLists { get; set; }

    DbSet<EmailEntity> Emails { get; set; }

    DbSet<WalletEntity> Wallets { get; set; }

    DbSet<CategoryEntity> Categories { get; set; }

    DbSet<TransactionEntity> Transactions { get; set; }

    DbSet<TransferEntity> Transfers { get; set; }

    DbSet<TimeSheetEntity> TimeSheets { get; set; }

    DbSet<NotificationEntity> Notifications { get; set; }

    DbSet<IconEntity> Icons { get; set; }

    DbSet<GoldPriceEntity> GoldPrices { get; set; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
