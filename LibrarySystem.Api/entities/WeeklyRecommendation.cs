namespace LibrarySystem.Api.Entities;

public class WeeklyRecommendation : BaseEntity
{
    public string BookTitle { get; set; } = string.Empty;
    public string Idea { get; set; } = string.Empty;
    public int AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }

    public User? AuthorUser { get; set; }
}
