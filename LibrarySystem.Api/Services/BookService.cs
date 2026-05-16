using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class BookService : IBookService
{
    private readonly LibraryDbContext _context;

    public BookService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<List<Book>> GetAllBooksAsync(GetBooksQuery? query = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> bookQuery = _context.Books
            .AsNoTracking()
            .Where(book => !book.IsDeleted);

        if (query is not null)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                bookQuery = bookQuery.Where(book =>
                    book.Title.Contains(search)
                    || book.Author.Contains(search)
                    || book.Isbn.Contains(search));
            }

            if (query.PublishYear.HasValue)
            {
                bookQuery = bookQuery.Where(book => book.PublishYear == query.PublishYear.Value);
            }

            if (query.IsAvailable.HasValue)
            {
                bookQuery = bookQuery.Where(book => book.IsAvailable == query.IsAvailable.Value);
            }
        }

        var books = await bookQuery
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Author)
            .ToListAsync(cancellationToken);

        if (query is not null
            && !string.IsNullOrWhiteSpace(query.Genre)
            && Enum.TryParse<GenreType>(query.Genre.Trim(), ignoreCase: true, out var genreFilter))
        {
            books = books
                .Where(book => book.Genres.Contains(genreFilter))
                .ToList();
        }

        return books;
    }

    public async Task<Book?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(book => book.Id == id && !book.IsDeleted, cancellationToken);
    }

    public async Task<int> AddBookAsync(Book newBook, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        newBook.Title = newBook.Title.Trim();
        newBook.Author = newBook.Author.Trim();
        newBook.Genres = newBook.Genres.Distinct().ToList();
        newBook.CreatedBy = actorUserId ?? 0;

        _context.Books.Add(newBook);
        await _context.SaveChangesAsync(cancellationToken);

        newBook.Isbn = IsbnGenerator.GenerateForBookId(newBook.Id);
        await _context.SaveChangesAsync(cancellationToken);
        return newBook.Id;
    }

    public async Task<bool> UpdateBookAsync(int id, UpdateBookRequest request, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (book is null)
        {
            return false;
        }

        if (HasMeaningfulValue(request.Title))
        {
            book.Title = request.Title!.Trim();
        }

        if (HasMeaningfulValue(request.Author))
        {
            book.Author = request.Author!.Trim();
        }

        if (request.Genres is { Count: > 0 })
        {
            var (_, parsedGenres) = GenreTypeListConverter.ValidateAndParseNames(request.Genres, required: true);
            book.Genres = parsedGenres;
        }

        if (request.PublishYear.HasValue)
        {
            book.PublishYear = request.PublishYear.Value;
        }

        if (request.IsAvailable.HasValue)
        {
            book.IsAvailable = request.IsAvailable.Value;
        }

        book.UpdatedBy = actorUserId ?? book.UpdatedBy ?? 0;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteBookAsync(int id, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (book is null)
        {
            return false;
        }

        book.IsDeleted = true;
        book.UpdatedBy = actorUserId ?? book.UpdatedBy ?? 0;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool HasMeaningfulValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value.Trim(), "string", StringComparison.OrdinalIgnoreCase);
    }
}
