using Microsoft.EntityFrameworkCore;

namespace Imageprocessing.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ImageJob> ImageJobs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ImageJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.status).IsRequired().HasConversion<string>();
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OriginalPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired().HasMaxLength(1000);
            });
        }
    }
}
