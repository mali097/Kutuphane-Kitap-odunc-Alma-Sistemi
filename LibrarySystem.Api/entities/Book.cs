using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Entities
{
    public class Book : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public bool IsAvailable { get; set; } = true;

        public ICollection<BorrowRecord>? BorrowRecords { get; set; }
    }
}