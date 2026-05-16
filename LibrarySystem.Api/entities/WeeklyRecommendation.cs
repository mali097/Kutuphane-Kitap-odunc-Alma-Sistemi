namespace LibrarySystem.Api.Entities;

public class WeeklyRecommendation : BaseEntity
{
    public int AuthorUserId { get; set; }
    public User? Author { get; set; }

    public string BookTitle { get; set; } = string.Empty;
    public string Idea { get; set; } = string.Empty;

    /// <summary>UTC date of the Monday that starts the recommendation week.</summary>
    public DateTime WeekStartUtc { get; set; }
}
