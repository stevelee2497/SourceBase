using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Identity;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, IConfiguration configuration) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IDbContext
{
    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.AddInterceptors(new ApplicationDbContextHistoryInterceptor(currentUser)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new ApplicationDbContextAuditInterceptor(currentUser)); // Audit trailing for create/update/delete actions
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoItemEntity>()
            .Property(todoItem => todoItem.Status)
            .HasConversion(new EnumToStringConverter<ItemStatus>())
            .HasMaxLength(50);

        // Identity table mappings
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
    }

    #region SeedData

    public static void SeedData(DbContext context, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>() ?? throw new Exception("Unable to bind AppSettings");

        foreach (var role in appSettings.Roles)
        {
            var existingRole = context.Set<ApplicationRole>().FirstOrDefault(b => b.Name == role);
            if (existingRole == null)
            {
                existingRole = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                context.Set<ApplicationRole>().Add(existingRole);
            }
            context.SaveChanges();
        }

        var adminUser = context.Set<ApplicationUser>().FirstOrDefault(b => b.Email == appSettings.AdminEmail);
        if (adminUser == null)
        {
            var adminUserEntity = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = appSettings.AdminEmail,
                NormalizedUserName = appSettings.AdminEmail.ToUpper(),
                Email = appSettings.AdminEmail,
                NormalizedEmail = appSettings.AdminEmail.ToUpper(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(null!, appSettings.AdminPassword),
            };
            context.Set<ApplicationUser>().Add(adminUserEntity);
            context.SaveChanges();

            // Assign all roles to admin
            foreach (var role in context.Set<ApplicationRole>().ToList())
            {
                context.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid>
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
