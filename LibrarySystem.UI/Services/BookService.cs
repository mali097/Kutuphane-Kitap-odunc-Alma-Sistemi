using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class BookService : IBookService
{
    public Task<bool> AddBookAsync(Book book)
    {
        return Task.FromResult(true);
    }

    public Task<bool> UpdateBookAsync(Book book)
    {
        return Task.FromResult(true);
    }

    public Task<List<Book>> GetAllBooksAsync()
    {
        return Task.FromResult(new List<Book>());
    }

    public Task<List<FavoriteBook>> GetFavoritesAsync(int userId)
    {
        return Task.FromResult(new List<FavoriteBook>());
    }

    public Task<bool> AddFavoriteAsync(int userId, int bookId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RemoveFavoriteAsync(int userId, int bookId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> DeleteBookAsync(int bookId)
    {
        return Task.FromResult(true);
    }
}
