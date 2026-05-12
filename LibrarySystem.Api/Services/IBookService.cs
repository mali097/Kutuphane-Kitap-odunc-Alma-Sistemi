using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services
{
    public interface IBookService
    {
        Task<List<Book>> GetAllBooksAsync(GetBooksQuery? query = null, CancellationToken cancellationToken = default);
        Task<Book?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> AddBookAsync(Book newBook, int? actorUserId = null, CancellationToken cancellationToken = default);
        Task<bool> UpdateBookAsync(int id, UpdateBookRequest request, int? actorUserId = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteBookAsync(int id, int? actorUserId = null, CancellationToken cancellationToken = default);
    }
}