namespace LibrarySystem.Api.Entities;

public class BookRating : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public decimal Score { get; set; }
}
