using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.DbContexts
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<TodoItemEntity> TodoItems { get; set; }

        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=SourceBase;TrustServerCertificate=True;User ID=sa;Password=Root@123");
        }
    }
}
