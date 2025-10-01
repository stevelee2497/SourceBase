using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.DbContexts;

[ScopedDependency<IDbContext>]
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserContext userContext, IConfiguration configuration) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
{
    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(userContext)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(userContext)); // Audit trailing for create/update/delete actions
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity table mappings
        modelBuilder.Entity<UserEntity>().ToTable("Users").HasMany(e => e.Roles).WithMany(e => e.Users).UsingEntity<IdentityUserRole<Guid>>();
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<RoleEntity>().ToTable("Roles");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
    }

    #region SeedData

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
                Roles = [.. context.Set<RoleEntity>()]
            };
            context.Set<UserEntity>().Add(adminUserEntity);
            context.SaveChanges();
        }
    }

    #endregion
}
