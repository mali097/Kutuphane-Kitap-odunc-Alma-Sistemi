namespace LibrarySystem.Api.Contracts;

public sealed class BorrowBookRequest
{
    public int UserId { get; init; }
    public int BookId { get; init; }
    public DateTime BorrowDate { get; init; } = DateTime.UtcNow;
    public DateTime ExpectedReturnDate { get; init; }
}
