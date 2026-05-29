using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, IConfiguration configuration, ILogger<ApplicationDbContextLoggingInterceptor> dbCommandLogger)
    : DbContext(options), IDbContext
{
    public DbSet<UserEntity> Users { get; set; }

    public DbSet<RoleEntity> Roles { get; set; }

    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    public DbSet<TodoListEntity> TodoLists { get; set; }

    public DbSet<EmailEntity> Emails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlite(connectionString).UseSeeding((context, _) => SeedData(context, configuration)).UseAsyncSeeding(async (context, _, _) => SeedData(context, configuration));
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(currentUser)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(currentUser)); // Audit trailing for create/update/delete actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextLoggingInterceptor(dbCommandLogger));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Globally forces every string column in SQLite to be case-insensitive
        configurationBuilder.Properties<string>().UseCollation("NOCASE");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }

    #endregion
}
