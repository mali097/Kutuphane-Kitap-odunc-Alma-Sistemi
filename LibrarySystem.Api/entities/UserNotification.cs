namespace LibrarySystem.Api.Entities;

public sealed class UserNotification : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int? BorrowRecordId { get; set; }
    public BorrowRecord? BorrowRecord { get; set; }

    public BorrowNotificationType NotificationType { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public bool IsRead { get; set; }
}
