using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Services;

public interface IBorrowNotificationService
{
    Task CreateBorrowReceivedAsync(BorrowRecord record, string bookTitle, CancellationToken cancellationToken = default);
    Task CreateReturnAsync(BorrowRecord record, string bookTitle, DateTime actualReturnDate, CancellationToken cancellationToken = default);
    Task<List<UserNotification>> GetUserNotificationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
    Task ProcessDueDateRemindersAsync(CancellationToken cancellationToken = default);
}
