namespace LibrarySystem.Api.Contracts;

public sealed class CreateWeeklyRecommendationRequest
{
    public string BookTitle { get; init; } = string.Empty;
    public string Idea { get; init; } = string.Empty;
}
