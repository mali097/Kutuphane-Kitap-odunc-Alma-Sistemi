using System.Collections.Concurrent;
using System.Threading;
using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
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

    public async Task<WeeklyRecommendationCreatedResponse> AddRecommendationAsync(
        int authorUserId,
        CreateWeeklyRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var weekStart = GetUtcWeekStartMonday(DateTime.UtcNow);
        var entity = new WeeklyRecommendation
        {
            AuthorUserId = authorUserId,
            BookTitle = request.BookTitle.Trim(),
            Idea = request.Idea.Trim(),
            WeekStartUtc = weekStart,
            CreatedBy = authorUserId
        };

        _context.WeeklyRecommendations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new WeeklyRecommendationCreatedResponse { RecommendationId = entity.Id };
    }

    public async Task<IReadOnlyList<WeeklyRecommendationResponse>> GetCurrentWeekRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        var weekStart = GetUtcWeekStartMonday(DateTime.UtcNow);
        var rows = await _context.WeeklyRecommendations
            .AsNoTracking()
            .Where(item => !item.IsDeleted && item.WeekStartUtc == weekStart)
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync(cancellationToken);

        return rows
            .Select(item => new WeeklyRecommendationResponse
    {
                RecommendationId = item.Id,
                BookTitle = item.BookTitle,
                Idea = item.Idea,
                AuthorUserId = item.AuthorUserId
            })
            .ToList();
    }

    private static DateTime GetUtcWeekStartMonday(DateTime utcNow)
    {
        var date = utcNow.Kind == DateTimeKind.Utc ? utcNow.Date : DateTime.UtcNow.Date;
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return DateTime.SpecifyKind(date.AddDays(-daysFromMonday), DateTimeKind.Utc);
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
