using System.Net.Http.Json;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public sealed class RecommendationService : IRecommendationService
{
    private const int MaxIdeaLength = 500;
    private readonly HttpClient _httpClient;

    public RecommendationService()
    {
        _httpClient = ApiClientHelper.CreateClient();
    }

    public async Task<List<AuthorRecommendation>> GetWeeklyRecommendationsAsync()
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var items = await _httpClient.GetFromJsonAsync<List<ApiWeeklyRecommendationDto>>("/api/recommendations/weekly");
            if (items is null || items.Count == 0)
            {
                return new();
            }

            return items.Select(Map).OrderByDescending(item => item.CreatedAt).ToList();
        }
        catch
        {
            return new();
        }
    }

    public async Task<AuthorRecommendation?> AddRecommendationAsync(string bookTitle, string idea)
    {
        if (!SessionHelper.IsAuthor)
        {
            return null;
        }

        var trimmedTitle = bookTitle.Trim();
        var trimmedIdea = idea.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle) || string.IsNullOrWhiteSpace(trimmedIdea))
        {
            return null;
        }

        if (trimmedIdea.Length > MaxIdeaLength)
        {
            trimmedIdea = trimmedIdea[..MaxIdeaLength];
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.PostAsJsonAsync("/api/author/recommendations", new
            {
                BookTitle = trimmedTitle,
                Idea = trimmedIdea
            });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var created = await response.Content.ReadFromJsonAsync<ApiWeeklyRecommendationDto>();
            return created is null ? null : Map(created);
        }
        catch
        {
            return null;
        }
    }

    private static AuthorRecommendation Map(ApiWeeklyRecommendationDto dto) => new()
    {
        RecommendationId = dto.RecommendationId,
        BookTitle = dto.BookTitle ?? string.Empty,
        Idea = dto.Idea ?? string.Empty,
        AuthorName = dto.AuthorName ?? string.Empty,
        CreatedAt = dto.CreatedAt
    };

    private sealed class ApiWeeklyRecommendationDto
    {
        public int RecommendationId { get; set; }
        public string? BookTitle { get; set; }
        public string? Idea { get; set; }
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
