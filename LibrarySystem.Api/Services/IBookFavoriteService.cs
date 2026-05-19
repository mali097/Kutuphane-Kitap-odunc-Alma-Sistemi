namespace LibrarySystem.Api.Services;

public interface IBookFavoriteService
{
    Task<FavoriteBookResult> AddFavoriteAsync(int userId, int bookId, CancellationToken cancellationToken = default);

    Task<FavoriteBookResult> RemoveFavoriteAsync(int userId, int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteBookItem>> GetFavoritesAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed record FavoriteBookResult(bool IsSuccess, string? ErrorMessage, bool AlreadyInState = false);

public sealed record FavoriteBookItem(
    int BookId,
    string Title,
    string Author,
    string Isbn,
    IReadOnlyList<string> Genres,
    int PublishYear,
    bool IsAvailable,
    DateTime FavoritedAt);
