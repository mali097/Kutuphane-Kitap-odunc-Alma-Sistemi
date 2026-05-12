using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IWeeklyRecommendationService
{
    Task<WeeklyRecommendationResponse> AddRecommendationAsync(
        int authorUserId,
        CreateWeeklyRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<List<WeeklyRecommendationResponse>> GetCurrentWeekRecommendationsAsync(
        CancellationToken cancellationToken = default);
}
