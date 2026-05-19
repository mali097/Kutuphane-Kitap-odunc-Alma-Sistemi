namespace LibrarySystem.Api.Contracts;

public sealed record WeeklyRecommendationResponse(
    int RecommendationId,
    string BookTitle,
    string Idea,
    int AuthorUserId,
    string AuthorName,
    DateTime CreatedAt,
    DateTime WeekStartDate,
    DateTime WeekEndDate
);
