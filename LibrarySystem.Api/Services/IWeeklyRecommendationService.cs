using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IWeeklyRecommendationService
{
    Task<WeeklyRecommendationCreatedResponse> AddRecommendationAsync(
        int authorUserId,
        CreateWeeklyRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeeklyRecommendationResponse>> GetCurrentWeekRecommendationsAsync(
        CancellationToken cancellationToken = default);
}
