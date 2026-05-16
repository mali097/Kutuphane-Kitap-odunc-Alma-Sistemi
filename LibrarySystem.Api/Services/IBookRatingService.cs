namespace LibrarySystem.Api.Services;

public interface IBookRatingService
{
    Task<RateBookResult> RateBookAsync(int userId, int bookId, decimal score, CancellationToken cancellationToken = default);
    Task<BookRatingSummary> GetBookRatingSummaryAsync(int bookId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, BookRatingSummary>> GetBookRatingSummariesAsync(IEnumerable<int> bookIds, CancellationToken cancellationToken = default);
    Task<List<UserRatedBookItem>> GetUserRatedBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<TopRatedBookItem>> GetTopRatedBooksAsync(int limit, CancellationToken cancellationToken = default);
}

public sealed record RateBookResult(bool IsSuccess, string? ErrorMessage, decimal? AverageRating, int RatingCount);

public sealed record BookRatingSummary(decimal? AverageRating, int RatingCount);

public sealed record UserRatedBookItem(
    int BookId,
    string Title,
    string Author,
    IReadOnlyList<string> Genres,
    decimal MyRating,
    decimal? AverageRating,
    int RatingCount,
    DateTime RatedAt);

public sealed record TopRatedBookItem(
    int BookId,
    string Title,
    string Author,
    IReadOnlyList<string> Genres,
    int PublishYear,
    decimal AverageRating,
    int RatingCount);
