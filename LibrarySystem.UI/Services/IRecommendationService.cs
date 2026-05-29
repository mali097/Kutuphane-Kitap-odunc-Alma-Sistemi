using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IRecommendationService
{
    Task<List<AuthorRecommendation>> GetWeeklyRecommendationsAsync();
    Task<AuthorRecommendation?> AddRecommendationAsync(string bookTitle, string idea);
}
