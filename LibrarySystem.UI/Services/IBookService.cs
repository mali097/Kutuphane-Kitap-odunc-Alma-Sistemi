using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IBookService
{
    Task<bool> AddBookAsync(Book book);
    Task<bool> UpdateBookAsync(Book book);
}
