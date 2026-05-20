namespace LibrarySystem.Api.Contracts;

public sealed class UserNotificationResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string NotificationType { get; init; } = string.Empty;
    public bool IsRead { get; init; }
}
