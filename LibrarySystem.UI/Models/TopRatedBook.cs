namespace LibrarySystem.UI.Models;

public class TopRatedBook
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
}
