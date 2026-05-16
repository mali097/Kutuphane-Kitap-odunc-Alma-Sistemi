namespace LibrarySystem.Api.Contracts;

public sealed class WeeklyRecommendationCreatedResponse
{
    public int RecommendationId { get; init; }
}

public sealed class WeeklyRecommendationResponse
{
    public int RecommendationId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string Idea { get; init; } = string.Empty;
    public int AuthorUserId { get; init; }
}
