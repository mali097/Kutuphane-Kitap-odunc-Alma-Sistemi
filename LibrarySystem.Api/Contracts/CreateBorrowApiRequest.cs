namespace LibrarySystem.Api.Contracts;

public sealed class CreateBorrowApiRequest
{
    public int BookId { get; init; }
    public int UserId { get; init; }
    public DateTime? DueDate { get; init; }
}
