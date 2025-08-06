using Microsoft.EntityFrameworkCore;

namespace TaskManagerApi.DATA
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
        {

        }

        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);                
                entity.Property(e => e.CreatedAt).IsRequired();
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
