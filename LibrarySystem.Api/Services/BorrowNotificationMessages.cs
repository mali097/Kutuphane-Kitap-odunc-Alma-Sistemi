namespace LibrarySystem.Api.Services;

public static class BorrowNotificationMessages
{
    public static string BookReceived(DateTime borrowDate, string bookTitle, DateTime dueDate) =>
        $"{borrowDate:dd.MM.yyyy} tarihinde \"{bookTitle}\" kitabını teslim aldınız. {dueDate:dd.MM.yyyy} tarihine kadar ({BorrowPolicies.LoanPeriodDays} gün içinde) kitabı teslim etmeniz gerekiyor.";

    public static string ReturnedOnTime(DateTime returnDate, string bookTitle) =>
        $"{returnDate:dd.MM.yyyy} tarihinde \"{bookTitle}\" kitabını zamanında teslim ettiniz.";

    public static string ReturnedLate(DateTime returnDate, string bookTitle, int daysLate) =>
        $"{returnDate:dd.MM.yyyy} tarihinde \"{bookTitle}\" kitabını {daysLate} gün geç teslim ettiniz.";

    public static string DueDateReminder(string bookTitle, DateTime dueDate) =>
        $"\"{bookTitle}\" kitabının son teslim tarihine {BorrowPolicies.DueDateReminderDaysBefore} gün kaldı. Son teslim: {dueDate:dd.MM.yyyy}.";
}
