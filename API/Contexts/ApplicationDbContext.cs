using API.Interceptors;
using Core.Constants;
using Core.Contexts;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
{
    public required DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public required DbSet<TodoItemEntity> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(AppSettingKeys.ConnectionString);

        optionsBuilder.AddInterceptors(new AuditingInterceptor(httpContextAccessor)); // Audit trailing for create/update/delete actions

        optionsBuilder.UseSeeding((context, _) =>
        {
            var adminRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Core.Constants.Roles.Admin);
            if (adminRole == null)
            {
                context.Set<RoleEntity>().Add(new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Core.Constants.Roles.Admin,
                    NormalizedName = Core.Constants.Roles.Admin.ToUpper(),
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                context.SaveChanges();
            }
            var userRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Core.Constants.Roles.User);
            if (userRole == null)
            {
                context.Set<RoleEntity>().Add(new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Core.Constants.Roles.User,
                    NormalizedName = Core.Constants.Roles.User.ToUpper(),
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                context.SaveChanges();
            }
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>()
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity<IdentityUserRole<Guid>>();
    }
}
