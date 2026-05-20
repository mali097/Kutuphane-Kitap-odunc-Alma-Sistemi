namespace LibrarySystem.Api.Contracts;

public sealed class BorrowRecordListResponse
{
    public int Id { get; init; }
    public int BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string BookAuthor { get; init; } = string.Empty;
    public string BookPublisher { get; init; } = string.Empty;
    public int BookPageCount { get; init; }
    public int UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public DateTime BorrowDate { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public bool IsReturned { get; init; }
}
