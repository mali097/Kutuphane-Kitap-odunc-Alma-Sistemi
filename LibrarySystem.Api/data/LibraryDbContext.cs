using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<WeeklyRecommendation> WeeklyRecommendations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WeeklyRecommendation>(entity =>
            {
                entity.HasIndex(item => new { item.WeekStartDate, item.WeekEndDate });
                entity.HasOne(item => item.AuthorUser)
                    .WithMany()
                    .HasForeignKey(item => item.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ApplyAuditInformation()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = utcNow;
                    continue;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = utcNow;
                    // CreatedDate alanının istemeden değiştirilmesini engeller.
                    entry.Property(entity => entity.CreatedDate).IsModified = false;
                }
            }
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}