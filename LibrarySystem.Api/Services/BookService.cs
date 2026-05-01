using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services
{
    public class BookService : IBookService
    {
        private readonly LibraryDbContext _context;

        public BookService(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<List<Book>> GetAllBooksAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .Where(book => !book.IsDeleted)
                .OrderBy(book => book.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task<Book?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(book => book.Id == id && !book.IsDeleted, cancellationToken);
        }

        public async Task<int> AddBookAsync(Book newBook, CancellationToken cancellationToken = default)
        {
            newBook.Title = newBook.Title.Trim();
            newBook.Author = newBook.Author.Trim();
            newBook.Isbn = newBook.Isbn.Trim();
            newBook.Genre = newBook.Genre.Trim();

            _context.Books.Add(newBook);
            await _context.SaveChangesAsync(cancellationToken);
            return newBook.Id;
        }

        public async Task<bool> DeleteBookAsync(int id, CancellationToken cancellationToken = default)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

            if (book is null)
            {
                return false;
            }

            book.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
