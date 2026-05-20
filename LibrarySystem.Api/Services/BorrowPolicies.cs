namespace LibrarySystem.Api.Services;

public static class BorrowPolicies
{
    public const int LoanPeriodDays = 15;
    public const int DueDateReminderDaysBefore = 3;

    public static DateTime CalculateDueDate(DateTime borrowDateUtc)
        => borrowDateUtc.Date.AddDays(LoanPeriodDays).AddHours(23).AddMinutes(59);
}
