using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class BookService : IBookService
{
    public Task<bool> AddBookAsync(Book book)
    {
        // Minimal stub to keep UI compiling; wire to API later.
        return Task.FromResult(true);
    }

    public Task<bool> UpdateBookAsync(Book book)
    {
        // Minimal stub to keep UI compiling; wire to API later.
        return Task.FromResult(true);
    }
}
