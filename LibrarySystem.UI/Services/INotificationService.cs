using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface INotificationService
{
    Task<List<DeliveryNotificationItem>> GetUserNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
}
