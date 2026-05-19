using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IBookService
{
    Task<bool> AddBookAsync(Book book);
    Task<bool> UpdateBookAsync(Book book);

    Task<List<Book>> GetAllBooksAsync();
    Task<List<FavoriteBook>> GetFavoritesAsync(int userId);
    Task<bool> AddFavoriteAsync(int userId, int bookId);
    Task<bool> RemoveFavoriteAsync(int userId, int bookId);
    Task<bool> DeleteBookAsync(int bookId);
}
