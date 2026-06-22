using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using SourceBase.Domain.Entities;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, IDateTime dateTime) : DbContext(options), IDbContext
{
    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<UserEntity> Users { get; set; }

    public DbSet<RoleEntity> Roles { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    public DbSet<TodoListEntity> TodoLists { get; set; }

    public DbSet<EmailEntity> Emails { get; set; }

    public DbSet<WalletEntity> Wallets { get; set; }

    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<TransactionEntity> Transactions { get; set; }

    public DbSet<TransferEntity> Transfers { get; set; }

    public DbSet<TimeSheetEntity> TimeSheets { get; set; }

    public DbSet<NotificationEntity> Notifications { get; set; }

    public DbSet<IconEntity> Icons { get; set; }

    public DbSet<GoldPriceEntity> GoldPrices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(currentUser, dateTime));
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(currentUser, dateTime));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            configurationBuilder.Properties<string>().UseCollation("NOCASE");
        else if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            configurationBuilder.Properties<string>().UseCollation("case_insensitive");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.HasCollation("case_insensitive", locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        }

        // Convert all enums to strings in the database
        SetEnumStringConverter(modelBuilder);

        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

        modelBuilder.Entity<GoldPriceEntity>()
            .HasIndex(x => new { x.Source, x.RecordedAt })
            .IsUnique();

    }

    #region Helper Methods

    private static void SetEnumStringConverter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;

                // Handle nullable enums
                var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                if (!enumType.IsEnum)
                    continue;

                var converterType = typeof(EnumToStringConverter<>).MakeGenericType(enumType);

                var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                property.SetValueConverter(converter);
            }
        }
    }

    public static void SeedData(DbContext context, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>() ?? throw new Exception("Unable to bind AppSettings");

        foreach (var role in appSettings.Roles)
        {
            var existingRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == role);
            if (existingRole == null)
            {
                existingRole = new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    Description = $"{role} role"
                };
                context.Set<RoleEntity>().Add(existingRole);
            }
            context.SaveChanges();
        }

        var adminUser = context.Set<UserEntity>().FirstOrDefault(b => b.Email == appSettings.AdminEmail);
        if (adminUser == null)
        {
            var adminUserEntity = new UserEntity
            {
                Id = Guid.NewGuid(),
                UserName = appSettings.AdminEmail,
                Email = appSettings.AdminEmail,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null!, appSettings.AdminPassword),
                Roles = [.. context.Set<RoleEntity>()],
            };
            context.Set<UserEntity>().Add(adminUserEntity);
            context.SaveChanges();
        }

        SeedCategories(context);
        SeedIcons(context);
    }

    private static void SeedCategories(DbContext context)
    {
        var defaultCategories = new List<(string Name, CategoryType Type, string Icon)>
        {
            ("Salary", CategoryType.Income, "💼"),
            ("Freelance", CategoryType.Income, "💻"),
            ("Investment", CategoryType.Income, "📈"),
            ("Gift", CategoryType.Income, "🎁"),
            ("Other Income", CategoryType.Income, "💰"),
            ("Food & Drink", CategoryType.Expense, "🍔"),
            ("Transport", CategoryType.Expense, "🚗"),
            ("Shopping", CategoryType.Expense, "🛍️"),
            ("Bills & Utilities", CategoryType.Expense, "💡"),
            ("Health", CategoryType.Expense, "❤️"),
            ("Entertainment", CategoryType.Expense, "🎬"),
            ("Education", CategoryType.Expense, "📚"),
            ("Travel", CategoryType.Expense, "✈️"),
            ("Other Expense", CategoryType.Expense, "📦"),
        };

        foreach (var (name, type, icon) in defaultCategories)
        {
            var exists = context.Set<CategoryEntity>().Any(c => c.IsSystem && c.Name == name && c.Type == type);
            if (!exists)
            {
                context.Set<CategoryEntity>().Add(new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Type = type,
                    Icon = icon,
                    IsSystem = true,
                    UserId = null
                });
            }
        }
        context.SaveChanges();
    }

    private static void SeedIcons(DbContext context)
    {
        if (context.Set<IconEntity>().Any())
            return;

        var icons = new List<(string Value, string Name, IconGroup Group, int SortOrder)>
        {
            ("💳", "Credit Card", IconGroup.Wallet, 1),
            ("🏦", "Bank", IconGroup.Wallet, 2),
            ("💵", "Cash", IconGroup.Wallet, 3),
            ("👛", "Wallet", IconGroup.Wallet, 4),
            ("💰", "Savings", IconGroup.Wallet, 5),
            ("📈", "Investment", IconGroup.Wallet, 6),
            ("🏠", "Property", IconGroup.Wallet, 7),
            ("🚀", "Goals", IconGroup.Wallet, 8),
            ("💎", "Premium", IconGroup.Wallet, 9),
            ("🏧", "ATM", IconGroup.Wallet, 10),
            ("💹", "Trading", IconGroup.Wallet, 11),
            ("🌏", "Foreign", IconGroup.Wallet, 12),
            ("🍔", "Food", IconGroup.Category, 1),
            ("☕", "Cafe", IconGroup.Category, 2),
            ("🚗", "Transport", IconGroup.Category, 3),
            ("✈️", "Travel", IconGroup.Category, 4),
            ("🛍️", "Shopping", IconGroup.Category, 5),
            ("💡", "Utilities", IconGroup.Category, 6),
            ("❤️", "Health", IconGroup.Category, 7),
            ("🎬", "Entertainment", IconGroup.Category, 8),
            ("📚", "Education", IconGroup.Category, 9),
            ("🎁", "Gift", IconGroup.Category, 10),
            ("💻", "Freelance", IconGroup.Category, 11),
            ("💼", "Salary", IconGroup.Category, 12),
            ("📦", "Other", IconGroup.Category, 13),
            ("🛒", "Groceries", IconGroup.Category, 14),
            ("🏥", "Medical", IconGroup.Category, 15),
            ("🎓", "Tuition", IconGroup.Category, 16),
            ("⚽", "Sports", IconGroup.Category, 17),
            ("🐾", "Pets", IconGroup.Category, 18),
            ("🔧", "Maintenance", IconGroup.Category, 19),
            ("🎮", "Gaming", IconGroup.Category, 20),
            ("⭐", "Favourite", IconGroup.General, 1),
            ("📌", "Pinned", IconGroup.General, 2),
            ("🔔", "Alert", IconGroup.General, 3),
            ("🌟", "Special", IconGroup.General, 4),
            ("🏷️", "Tag", IconGroup.General, 5),
            ("💬", "Note", IconGroup.General, 6),
            ("🔑", "Key", IconGroup.General, 7),
            ("📊", "Stats", IconGroup.General, 8),
        };

        foreach (var (value, name, group, sortOrder) in icons)
        {
            context.Set<IconEntity>().Add(new IconEntity
            {
                Id = Guid.NewGuid(),
                Value = value,
                Name = name,
                Group = group,
                SortOrder = sortOrder,
                IsSystem = true,
            });
        }
        context.SaveChanges();
    }

    #endregion
}
