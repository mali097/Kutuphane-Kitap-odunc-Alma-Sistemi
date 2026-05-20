namespace LibrarySystem.Api.Entities;

public enum BorrowNotificationType
{
    BookReceived = 1,
    ReturnedOnTime = 2,
    ReturnedLate = 3,
    DueDateReminder = 4
}
