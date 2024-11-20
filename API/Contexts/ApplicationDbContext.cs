using Core.Contexts;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace API.Contexts
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
    {
        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public Guid? GetCurrentUserId() => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserEntity>()
                .HasMany(e => e.Roles)
                .WithMany(e => e.Users)
                .UsingEntity<IdentityUserRole<Guid>>();

            modelBuilder.Entity<RoleEntity>().HasData(
                new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Core.Constants.Roles.Admin,
                    NormalizedName = Core.Constants.Roles.Admin.ToUpper(),
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = Core.Constants.Roles.User,
                    NormalizedName = Core.Constants.Roles.User.ToUpper(),
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );
        }
    }
}
