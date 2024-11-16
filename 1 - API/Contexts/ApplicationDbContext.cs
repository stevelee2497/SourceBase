using Core.Contexts;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Contexts
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
    {
        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public Guid? GetCurrentUserId() => Guid.TryParse(httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    }
}
