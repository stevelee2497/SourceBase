using Api.Interceptors;
using Domain.Constants;
using Domain.Contexts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
{
    public required DbSet<AuditHistoryEntity> AuditHistories { get; set; }

    public required DbSet<TodoItemEntity> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(AppSettingKeys.ConnectionString);

        optionsBuilder.AddInterceptors(new HistoryInterceptor(httpContextAccessor)); // Audit history for all actions
        optionsBuilder.AddInterceptors(new AuditInterceptor(httpContextAccessor)); // Audit trailing for create/update/delete actions

        optionsBuilder.UseSeeding((context, _) =>
        {
            var adminRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Domain.Constants.Roles.Admin);
            if (adminRole == null)
            {
                context.Set<RoleEntity>().Add(new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Constants.Roles.Admin,
                    NormalizedName = Domain.Constants.Roles.Admin.ToUpper(),
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                context.SaveChanges();
            }
            var userRole = context.Set<RoleEntity>().FirstOrDefault(b => b.Name == Domain.Constants.Roles.User);
            if (userRole == null)
            {
                context.Set<RoleEntity>().Add(new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Constants.Roles.User,
                    NormalizedName = Domain.Constants.Roles.User.ToUpper(),
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
