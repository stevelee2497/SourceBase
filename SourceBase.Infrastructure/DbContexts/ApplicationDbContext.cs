using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.Identity;

namespace SourceBase.Infrastructure.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserContext userContext, IConfiguration configuration) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IDbContext
{
    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    IQueryable<UserEntity> IDbContext.Users => Set<ApplicationUser>()
        .Select(u => new UserEntity
        {
            Id = u.Id,
            UserName = u.UserName,
            NormalizedUserName = u.NormalizedUserName,
            Email = u.Email,
            NormalizedEmail = u.NormalizedEmail,
            EmailConfirmed = u.EmailConfirmed,
            PhoneNumber = u.PhoneNumber,
            FirstName = u.FirstName,
            LastName = u.LastName,
        });

    IQueryable<RoleEntity> IDbContext.Roles => Set<ApplicationRole>()
        .Select(r => new RoleEntity
        {
            Id = r.Id,
            Name = r.Name,
            NormalizedName = r.NormalizedName,
        });

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
