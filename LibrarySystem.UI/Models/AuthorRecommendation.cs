namespace LibrarySystem.UI.Models;

public sealed class AuthorRecommendation
{
    public int RecommendationId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Idea { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
