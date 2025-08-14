using Microsoft.EntityFrameworkCore;

namespace TaskManagerApi.DATA
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
        {

        }

        // Lazy loading disabled - use explicit Include() when needed

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Tasks)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);                
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });



            base.OnModelCreating(modelBuilder);
        }
    }
}
