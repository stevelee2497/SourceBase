using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SourceBase.Domain.Abstractions;
using SourceBase.Domain.Common;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.Interceptors;
using System.Data;

namespace SourceBase.Infrastructure.DbContexts;

[ScopedDependency<IDbContext>]
public class ApplicationDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>, IDbContext
{
    #region DbSets

    public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public DbSet<TodoItemEntity> TodoItems { get; set; }

    public DbSet<ProfileEntity> Profiles { get; set; }

    #endregion

    #region Ctor

    private readonly IHttpContextAccessor httpContextAccessor;

    public ApplicationDbContext()
    {
        httpContextAccessor = new HttpContextAccessor();
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Configuring

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=../app.db");

        optionsBuilder.AddInterceptors(new HistoryInterceptor(httpContextAccessor)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new AuditInterceptor(httpContextAccessor)); // Audit trailing for create/update/delete actions

        optionsBuilder.UseSeeding((context, _) =>
        {
            var adminEmail = "admin@yopmail.com";
            var adminRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Domain.Common.Roles.Admin);
            if (adminRole == null)
            {
                adminRole = new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Common.Roles.Admin,
                    NormalizedName = Domain.Common.Roles.Admin.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                context.Set<RoleEntity>().Add(adminRole);
                context.SaveChanges();
            }

            var userRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Domain.Common.Roles.User);
            if (userRole == null)
            {
                context.Set<RoleEntity>().Add(new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Common.Roles.User,
                    NormalizedName = Domain.Common.Roles.User.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                context.SaveChanges();
            }

            var adminUser = context.Set<UserEntity>().FirstOrDefault(b => b.Email == adminEmail);
            if (adminUser == null)
            {
                var adminUserEntity = new UserEntity
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    NormalizedUserName = adminEmail.ToUpper(),
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.ToUpper(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null!, "Admin@123"),
                    Roles = [adminRole],
                    Profile = new ProfileEntity
                    {
                        FirstName = "Admin",
                        LastName = "User",
                    }
                };
                context.Set<UserEntity>().Add(adminUserEntity);
                context.SaveChanges();
            }
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>().ToTable("Users").HasMany(e => e.Roles).WithMany(e => e.Users).UsingEntity<IdentityUserRole<Guid>>();
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<RoleEntity>().ToTable("Roles");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
    }

    #endregion
}
