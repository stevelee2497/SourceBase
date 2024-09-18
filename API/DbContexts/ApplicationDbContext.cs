using Core.DbContexts;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.DbContexts
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserEntity, RoleEntity, Guid>(options), IDbContext
    {
        #region DbSets

        public DbSet<AuditHistoryEntity> AuditHistories { get; set; }

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        #endregion

        #region Ctors

        public string CurrentUserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous user";

        #endregion
    }
}
