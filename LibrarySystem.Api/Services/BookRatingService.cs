using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class BookRatingService : IBookRatingService
{
    private readonly LibraryDbContext _context;

    public BookRatingService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<RateBookResult> RateBookAsync(int userId, int bookId, decimal score, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken);

        if (user is null)
        {
            return new RateBookResult(false, "User not found.", null, 0);
        }

        if (!IsEligibleRole(user.Role))
        {
            return new RateBookResult(false, "Only Student or Author users can rate books.", null, 0);
        }

        var bookExists = await _context.Books
            .AnyAsync(item => item.Id == bookId && !item.IsDeleted, cancellationToken);
        if (!bookExists)
        {
            return new RateBookResult(false, "Book not found.", null, 0);
        }

        var existingRating = await _context.BookRatings
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.BookId == bookId && !item.IsDeleted,
                cancellationToken);

        if (existingRating is null)
        {
            _context.BookRatings.Add(new BookRating
            {
                UserId = userId,
                BookId = bookId,
                Score = score,
                CreatedBy = userId
            });
        }
        else
        {
            existingRating.Score = score;
            existingRating.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var summary = await GetBookRatingSummaryAsync(bookId, cancellationToken);
        return new RateBookResult(true, null, summary.AverageRating, summary.RatingCount);
    }

    public async Task<BookRatingSummary> GetBookRatingSummaryAsync(int bookId, CancellationToken cancellationToken = default)
    {
        var summary = await _context.BookRatings
            .AsNoTracking()
            .Where(item => item.BookId == bookId && !item.IsDeleted)
            .GroupBy(item => item.BookId)
            .Select(group => new
            {
                AverageRating = group.Average(item => item.Score),
                RatingCount = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary is null
            ? new BookRatingSummary(null, 0)
            : new BookRatingSummary(decimal.Round(summary.AverageRating, 2), summary.RatingCount);
    }

    public async Task<Dictionary<int, BookRatingSummary>> GetBookRatingSummariesAsync(
        IEnumerable<int> bookIds,
        CancellationToken cancellationToken = default)
    {
        var ids = bookIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, BookRatingSummary>();
        }

        var summaries = await _context.BookRatings
            .AsNoTracking()
            .Where(item => !item.IsDeleted && ids.Contains(item.BookId))
            .GroupBy(item => item.BookId)
            .Select(group => new
            {
                BookId = group.Key,
                AverageRating = group.Average(item => item.Score),
                RatingCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        return summaries.ToDictionary(
            item => item.BookId,
            item => new BookRatingSummary(decimal.Round(item.AverageRating, 2), item.RatingCount));
    }

    public async Task<List<UserRatedBookItem>> GetUserRatedBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRatings = await _context.BookRatings
            .AsNoTracking()
            .Include(item => item.Book)
            .Where(item => item.UserId == userId && !item.IsDeleted && item.Book != null && !item.Book.IsDeleted)
            .OrderByDescending(item => item.UpdatedDate ?? item.CreatedDate)
            .ToListAsync(cancellationToken);

        var summaries = await GetBookRatingSummariesAsync(userRatings.Select(item => item.BookId), cancellationToken);

        return userRatings.Select(item =>
        {
            summaries.TryGetValue(item.BookId, out var summary);
            var book = item.Book!;
            return new UserRatedBookItem(
                item.BookId,
                book.Title,
                book.Author,
                GenreTypeListConverter.ToGenreNames(book.Genres),
                item.Score,
                summary?.AverageRating,
                summary?.RatingCount ?? 0,
                item.UpdatedDate ?? item.CreatedDate);
        }).ToList();
    }

    public async Task<List<TopRatedBookItem>> GetTopRatedBooksAsync(int limit, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);

        var ratings = await _context.BookRatings
            .AsNoTracking()
            .Where(item => !item.IsDeleted)
            .GroupBy(item => item.BookId)
            .Select(group => new
            {
                BookId = group.Key,
                AverageRating = group.Average(item => item.Score),
                RatingCount = group.Count()
            })
            .OrderByDescending(item => item.AverageRating)
            .ThenByDescending(item => item.RatingCount)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        var bookIds = ratings.Select(item => item.BookId).ToList();
        var books = await _context.Books
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.Id) && !book.IsDeleted)
            .ToDictionaryAsync(book => book.Id, cancellationToken);

        return ratings
            .Where(item => books.ContainsKey(item.BookId))
            .Select(item =>
            {
                var book = books[item.BookId];
                return new TopRatedBookItem(
                    item.BookId,
                    book.Title,
                    book.Author,
                    GenreTypeListConverter.ToGenreNames(book.Genres),
                    book.PublishYear,
                    decimal.Round(item.AverageRating, 2),
                    item.RatingCount);
            })
            .ToList();
    }

    private static bool IsEligibleRole(string role)
    {
        return string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Author", StringComparison.OrdinalIgnoreCase);
    }
}
