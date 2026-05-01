using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Services
{
    
    public interface IBookService
    {
        Task<List<Book>> GetAllBooksAsync(CancellationToken cancellationToken = default);
        Task<Book?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> AddBookAsync(Book newBook, CancellationToken cancellationToken = default);
        Task<bool> DeleteBookAsync(int id, CancellationToken cancellationToken = default);
    }
}