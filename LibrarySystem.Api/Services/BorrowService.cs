using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class BorrowService : IBorrowService
{
    private readonly LibraryDbContext _context;
    private readonly IBorrowNotificationService _notificationService;

    public BorrowService(LibraryDbContext context, IBorrowNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<int?> BorrowBookAsync(BorrowBookRequest request, int actingUserId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Id == request.UserId && !item.IsDeleted, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(item => item.Id == request.BookId && !item.IsDeleted, cancellationToken);
        if (book is null || !book.IsAvailable)
        {
            return null;
        }

        book.IsAvailable = false;

        var borrowDate = DateTime.UtcNow;
        var expectedReturnDate = BorrowPolicies.CalculateDueDate(borrowDate);

        var borrowRecord = new BorrowRecord
        {
            UserId = request.UserId,
            BookId = request.BookId,
            BorrowDate = borrowDate,
            ExpectedReturnDate = expectedReturnDate,
            IsReturned = false,
            CreatedBy = actingUserId
        };

        _context.BorrowRecords.Add(borrowRecord);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateBorrowReceivedAsync(borrowRecord, book.Title, cancellationToken);
        return borrowRecord.Id;
    }

    public async Task<BorrowRecord?> GetBorrowRecordAsync(int borrowRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.BorrowRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == borrowRecordId && !item.IsDeleted, cancellationToken);
    }

    public async Task<bool> ReturnBookAsync(
        ReturnBookRequest request,
        int actingUserId,
        bool allowAnyUser,
        CancellationToken cancellationToken = default)
    {
        var borrowRecord = await _context.BorrowRecords
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == request.BorrowRecordId && !item.IsDeleted, cancellationToken);

        if (borrowRecord is null || borrowRecord.IsReturned || borrowRecord.Book is null)
        {
            return false;
        }

        if (!allowAnyUser && borrowRecord.UserId != actingUserId)
        {
            return false;
        }

        var actualReturnDate = request.ActualReturnDate ?? DateTime.UtcNow;

        borrowRecord.IsReturned = true;
        borrowRecord.ActualReturnDate = actualReturnDate;
        borrowRecord.UpdatedBy = actingUserId;
        borrowRecord.Book.IsAvailable = true;

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.CreateReturnAsync(
            borrowRecord,
            borrowRecord.Book.Title,
            actualReturnDate,
            cancellationToken);

        return true;
    }

    public async Task<List<BorrowRecord>> GetBorrowRecordsAsync(
        int? userId,
        int? bookId,
        bool? isReturned,
        CancellationToken cancellationToken = default)
    {
        IQueryable<BorrowRecord> query = _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted);

        if (userId.HasValue)
        {
            query = query.Where(record => record.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(record => record.BookId == bookId.Value);
        }

        if (isReturned.HasValue)
        {
            query = query.Where(record => record.IsReturned == isReturned.Value);
        }

        return await query
            .OrderByDescending(record => record.BorrowDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BorrowRecord>> GetUserBorrowRecordsWithDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted && record.UserId == userId)
            .Include(record => record.User)
            .Include(record => record.Book)
            .OrderByDescending(record => record.BorrowDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BorrowRecord>> GetActiveUserBorrowRecordsWithDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted && record.UserId == userId && !record.IsReturned)
            .Include(record => record.User)
            .Include(record => record.Book)
            .OrderBy(record => record.ExpectedReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BorrowRecord>> GetOverdueBorrowRecordsWithDetailsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted
                && !record.IsReturned
                && record.ExpectedReturnDate < now)
            .Include(record => record.User)
            .Include(record => record.Book)
            .OrderBy(record => record.ExpectedReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BorrowRecord>> GetAllBorrowRecordsWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted)
            .Include(record => record.User)
            .Include(record => record.Book)
            .OrderByDescending(record => record.BorrowDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BorrowRecord>> GetActiveBorrowRecordsWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => !record.IsDeleted && !record.IsReturned)
            .Include(record => record.User)
            .Include(record => record.Book)
            .OrderBy(record => record.ExpectedReturnDate)
            .ToListAsync(cancellationToken);
    }
}
