using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class BookFavoriteService : IBookFavoriteService
{
    private readonly LibraryDbContext _context;

    public BookFavoriteService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<FavoriteBookResult> AddFavoriteAsync(int userId, int bookId, CancellationToken cancellationToken = default)
    {
        var roleError = await ValidateEligibleUserAsync(userId, cancellationToken);
        if (roleError is not null)
        {
            return new FavoriteBookResult(false, roleError);
        }

        var bookExists = await _context.Books
            .AnyAsync(item => item.Id == bookId && !item.IsDeleted, cancellationToken);
        if (!bookExists)
        {
            return new FavoriteBookResult(false, "Book not found.");
        }

        var existing = await _context.BookFavorites
            .FirstOrDefaultAsync(item => item.UserId == userId && item.BookId == bookId, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
            {
                return new FavoriteBookResult(true, null, AlreadyInState: true);
            }

            existing.IsDeleted = false;
            existing.UpdatedBy = userId;
            await _context.SaveChangesAsync(cancellationToken);
            return new FavoriteBookResult(true, null);
        }

        _context.BookFavorites.Add(new BookFavorite
        {
            UserId = userId,
            BookId = bookId,
            CreatedBy = userId
        });
        await _context.SaveChangesAsync(cancellationToken);
        return new FavoriteBookResult(true, null);
    }

    public async Task<FavoriteBookResult> RemoveFavoriteAsync(int userId, int bookId, CancellationToken cancellationToken = default)
    {
        var roleError = await ValidateEligibleUserAsync(userId, cancellationToken);
        if (roleError is not null)
        {
            return new FavoriteBookResult(false, roleError);
        }

        var favorite = await _context.BookFavorites
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.BookId == bookId && !item.IsDeleted,
                cancellationToken);

        if (favorite is null)
        {
            return new FavoriteBookResult(false, "Favorite not found.", AlreadyInState: true);
        }

        favorite.IsDeleted = true;
        favorite.UpdatedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return new FavoriteBookResult(true, null);
    }

    public async Task<IReadOnlyList<FavoriteBookItem>> GetFavoritesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roleError = await ValidateEligibleUserAsync(userId, cancellationToken);
        if (roleError is not null)
        {
            return Array.Empty<FavoriteBookItem>();
        }

        var favorites = await _context.BookFavorites
            .AsNoTracking()
            .Include(item => item.Book)
            .Where(item => item.UserId == userId && !item.IsDeleted && item.Book != null && !item.Book.IsDeleted)
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync(cancellationToken);

        return favorites
            .Select(item =>
            {
                var book = item.Book!;
                return new FavoriteBookItem(
                    item.BookId,
                    book.Title,
                    book.Author,
                    book.Isbn,
                    GenreTypeListConverter.ToGenreNames(book.Genres),
                    book.PublishYear,
                    book.IsAvailable,
                    item.CreatedDate);
            })
            .ToList();
    }

    private async Task<string?> ValidateEligibleUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken);

        if (user is null)
        {
            return "User not found.";
        }

        if (!IsEligibleRole(user.Role))
        {
            return "Only Student or Author users can manage favorites.";
        }

        return null;
    }

    private static bool IsEligibleRole(string role)
    {
        return string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Author", StringComparison.OrdinalIgnoreCase);
    }
}
