using Core.Entities;
using Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Core.DbContexts
{
    public class ApplicationDbContext : IdentityAuditDbContext
    {
        private readonly ISessionUserHelper _sessionUserHelper;

        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ISessionUserHelper sessionUserHelper) : base(options)
        {
            _sessionUserHelper = sessionUserHelper;
        }

        public override string GetAuthor()
        {
            return _sessionUserHelper.User;
        }
    }
}
