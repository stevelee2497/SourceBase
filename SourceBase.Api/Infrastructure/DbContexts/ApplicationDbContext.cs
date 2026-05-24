using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, IConfiguration configuration, ILogger<ApplicationDbContextLoggingInterceptor> dbCommandLogger)
    : IdentityDbContext<UserEntity, RoleEntity, Guid, IdentityUserClaim<Guid>, UserRoleEntity, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options), IDbContext
{
    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    public DbSet<EmailEntity> Emails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlite(connectionString).UseSeeding((context, _) => SeedData(context, configuration)).UseAsyncSeeding(async (context, _, _) => SeedData(context, configuration));
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(currentUser)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(currentUser)); // Audit trailing for create/update/delete actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextLoggingInterceptor(dbCommandLogger));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Convert all enums to strings in the database
        SetEnumStringConverter(modelBuilder);

        // Identity table mappings
        modelBuilder.Entity<UserEntity>().ToTable("Users");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<RoleEntity>().ToTable("Roles");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<UserRoleEntity>(builder =>
        {
            builder.ToTable("UserRoles");
            builder.HasKey(x => new { x.UserId, x.RoleId });
            builder.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);
            builder.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });
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
                    NormalizedName = role.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
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
                NormalizedUserName = appSettings.AdminEmail.ToUpper(),
                Email = appSettings.AdminEmail,
                NormalizedEmail = appSettings.AdminEmail.ToUpper(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null!, appSettings.AdminPassword),
            };
            context.Set<UserEntity>().Add(adminUserEntity);
            context.SaveChanges();

            // Assign all roles to admin
            foreach (var role in context.Set<RoleEntity>().ToList())
            {
                context.Set<UserRoleEntity>().Add(new UserRoleEntity
                {
                    UserId = adminUserEntity.Id,
                    RoleId = role.Id
                });
            }
            context.SaveChanges();
        }
    }

    #endregion
}
