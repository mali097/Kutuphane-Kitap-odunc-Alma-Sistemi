using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        public DbSet<BookRating> BookRatings { get; set; }
        public DbSet<WeeklyRecommendation> WeeklyRecommendations { get; set; }
        public DbSet<BookFavorite> BookFavorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BookFavorite>(entity =>
            {
                entity.HasIndex(item => new { item.UserId, item.BookId }).IsUnique();
                entity.HasOne(item => item.User)
                    .WithMany()
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Book)
                    .WithMany()
                    .HasForeignKey(item => item.BookId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WeeklyRecommendation>(entity =>
            {
                entity.HasOne(item => item.Author)
                    .WithMany()
                    .HasForeignKey(item => item.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BookRating>(entity =>
            {
                entity.HasIndex(item => new { item.UserId, item.BookId }).IsUnique();
                entity.Property(item => item.Score).HasPrecision(3, 1);
            });

            var genresComparer = new ValueComparer<List<GenreType>>(
                (left, right) => (left ?? new List<GenreType>()).SequenceEqual(right ?? new List<GenreType>()),
                genres => genres.Aggregate(0, (hash, genre) => HashCode.Combine(hash, genre)),
                genres => genres.ToList());

            modelBuilder.Entity<Book>(entity =>
            {
                entity.Property(book => book.Genres)
                    .HasColumnName("Genre")
                    .HasConversion(
                        genres => GenreTypeListConverter.ToStorage(genres),
                        value => GenreTypeListConverter.FromStorage(value))
                    .Metadata.SetValueComparer(genresComparer);
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