namespace LibrarySystem.Api.Services;

public static class BorrowPolicies
{
    public const int LoanPeriodDays = 15;
    public const int DueDateReminderDaysBefore = 3;

    public static DateTime CalculateDueDate(DateTime borrowDateUtc)
        => borrowDateUtc.Date.AddDays(LoanPeriodDays).AddHours(23).AddMinutes(59);

    public static DateTime ToEndOfDay(DateTime date)
        => date.Date.AddHours(23).AddMinutes(59);

    public static bool IsValidExpectedReturnDate(DateTime borrowDateUtc, DateTime expectedReturnDate)
    {
        var borrowDay = borrowDateUtc.Date;
        var dueDay = expectedReturnDate.Date;
        return dueDay >= borrowDay && dueDay <= borrowDay.AddDays(LoanPeriodDays);
    }
}
