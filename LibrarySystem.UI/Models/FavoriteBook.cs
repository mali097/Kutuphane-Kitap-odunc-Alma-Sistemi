namespace LibrarySystem.UI.Models;

public class FavoriteBook
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string BookCategory { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.Now;
}