using System.Collections.Concurrent;
using System.Threading;
using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class WeeklyRecommendationService : IWeeklyRecommendationService
{
    private static readonly ConcurrentDictionary<int, WeeklyRecommendationItem> Items = new();
    private static int _currentId;
    private readonly LibraryDbContext _context;

    public WeeklyRecommendationService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklyRecommendationResponse> AddRecommendationAsync(
        int authorUserId,
        CreateWeeklyRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == authorUserId && !item.IsDeleted, cancellationToken);

        if (author is null)
        {
            throw new InvalidOperationException("Author user not found.");
        }

        var createdAt = DateTime.UtcNow;
        var weekStart = GetWeekStart(createdAt);
        var weekEnd = weekStart.AddDays(6);

        var id = Interlocked.Increment(ref _currentId);
        var recommendationItem = new WeeklyRecommendationItem(
            id,
            request.BookTitle.Trim(),
            request.Idea.Trim(),
            authorUserId,
            $"{author.FirstName} {author.LastName}".Trim(),
            createdAt,
            weekStart,
            weekEnd);

        Items[id] = recommendationItem;
        return ToResponse(recommendationItem);
    }

    public Task<List<WeeklyRecommendationResponse>> GetCurrentWeekRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var weekStart = GetWeekStart(now);
        var weekEnd = weekStart.AddDays(6);

        var results = Items.Values
            .Where(item => item.WeekStartDate == weekStart && item.WeekEndDate == weekEnd)
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult(results);
    }

    private static WeeklyRecommendationResponse ToResponse(WeeklyRecommendationItem item)
    {
        return new WeeklyRecommendationResponse(
            item.RecommendationId,
            item.BookTitle,
            item.Idea,
            item.AuthorUserId,
            item.AuthorName,
            item.CreatedAt,
            item.WeekStartDate,
            item.WeekEndDate
        );
    }

    private static DateTime GetWeekStart(DateTime utcDateTime)
    {
        var date = utcDateTime.Date;
        var difference = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-difference);
    }

    private sealed record WeeklyRecommendationItem(
        int RecommendationId,
        string BookTitle,
        string Idea,
        int AuthorUserId,
        string AuthorName,
        DateTime CreatedAt,
        DateTime WeekStartDate,
        DateTime WeekEndDate
    );
}
