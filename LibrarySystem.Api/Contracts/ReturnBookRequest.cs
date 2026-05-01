namespace LibrarySystem.Api.Contracts;

public sealed class ReturnBookRequest
{
    public int BorrowRecordId { get; init; }
    public DateTime? ActualReturnDate { get; init; }
}
