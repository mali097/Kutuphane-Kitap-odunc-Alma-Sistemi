using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class BorrowNotificationService : IBorrowNotificationService
{
    private readonly LibraryDbContext _context;

    public BorrowNotificationService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task CreateBorrowReceivedAsync(
        BorrowRecord record,
        string bookTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = new UserNotification
        {
            UserId = record.UserId,
            BorrowRecordId = record.Id,
            NotificationType = BorrowNotificationType.BookReceived,
            Message = BorrowNotificationMessages.BookReceived(
                record.BorrowDate,
                bookTitle,
                record.ExpectedReturnDate),
            OccurredAt = record.BorrowDate,
            IsRead = false,
            CreatedBy = record.CreatedBy
        };

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateReturnAsync(
        BorrowRecord record,
        string bookTitle,
        DateTime actualReturnDate,
        CancellationToken cancellationToken = default)
    {
        var daysLate = (actualReturnDate.Date - record.ExpectedReturnDate.Date).Days;
        var isLate = daysLate > 0;

        var notification = new UserNotification
        {
            UserId = record.UserId,
            BorrowRecordId = record.Id,
            NotificationType = isLate
                ? BorrowNotificationType.ReturnedLate
                : BorrowNotificationType.ReturnedOnTime,
            Message = isLate
                ? BorrowNotificationMessages.ReturnedLate(actualReturnDate, bookTitle, daysLate)
                : BorrowNotificationMessages.ReturnedOnTime(actualReturnDate, bookTitle),
            OccurredAt = actualReturnDate,
            IsRead = false,
            CreatedBy = record.UpdatedBy ?? record.CreatedBy
        };

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserNotification>> GetUserNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(
                item => item.Id == notificationId && item.UserId == userId && !item.IsDeleted,
                cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.UpdatedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ProcessDueDateRemindersAsync(CancellationToken cancellationToken = default)
    {
        var reminderDate = DateTime.UtcNow.Date.AddDays(BorrowPolicies.DueDateReminderDaysBefore);

        var dueSoonRecords = await _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.Book)
            .Where(item => !item.IsDeleted
                && !item.IsReturned
                && item.ExpectedReturnDate.Date == reminderDate)
            .ToListAsync(cancellationToken);

        if (dueSoonRecords.Count == 0)
        {
            return;
        }

        var borrowRecordIds = dueSoonRecords.Select(item => item.Id).ToList();
        var alreadySentList = await _context.UserNotifications
            .AsNoTracking()
            .Where(item => !item.IsDeleted
                && item.NotificationType == BorrowNotificationType.DueDateReminder
                && item.BorrowRecordId.HasValue
                && borrowRecordIds.Contains(item.BorrowRecordId.Value))
            .Select(item => item.BorrowRecordId!.Value)
            .ToListAsync(cancellationToken);
        var alreadySent = alreadySentList.ToHashSet();

        foreach (var record in dueSoonRecords)
        {
            if (alreadySent.Contains(record.Id) || record.Book is null)
            {
                continue;
            }

            _context.UserNotifications.Add(new UserNotification
            {
                UserId = record.UserId,
                BorrowRecordId = record.Id,
                NotificationType = BorrowNotificationType.DueDateReminder,
                Message = BorrowNotificationMessages.DueDateReminder(
                    record.Book.Title,
                    record.ExpectedReturnDate),
                OccurredAt = DateTime.UtcNow,
                IsRead = false,
                CreatedBy = record.UserId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
