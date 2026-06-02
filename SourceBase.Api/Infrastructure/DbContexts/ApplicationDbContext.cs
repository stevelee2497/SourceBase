using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, ILogger<ApplicationDbContextLoggingInterceptor> dbCommandLogger)
    : DbContext(options), IDbContext
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(currentUser)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(currentUser)); // Audit trailing for create/update/delete actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextLoggingInterceptor(dbCommandLogger)); // Logs all executed SQL commands with parameters for debugging and performance monitoring
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            configurationBuilder.Properties<string>().UseCollation("NOCASE");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.HasCollation("case_insensitive", locale: "und-x-icu", provider: "icu", deterministic: false);
            modelBuilder.UseCollation("case_insensitive");
        }

        // Convert all enums to strings in the database
        SetEnumStringConverter(modelBuilder);

        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

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

    #endregion
}
