using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.DbContexts
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
    }
}
