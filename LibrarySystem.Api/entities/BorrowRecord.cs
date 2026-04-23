namespace LibrarySystem.Api.Entities
{
    public class BorrowRecord : BaseEntity
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public DateTime BorrowDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public bool IsReturned { get; set; } = false;
    }
}