using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class WeeklyRecommendationService : IWeeklyRecommendationService
{
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

        var recommendation = new WeeklyRecommendation
        {
            BookTitle = request.BookTitle.Trim(),
            Idea = request.Idea.Trim(),
            AuthorUserId = authorUserId,
            AuthorName = $"{author.FirstName} {author.LastName}".Trim(),
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            CreatedBy = authorUserId
        };

        _context.WeeklyRecommendations.Add(recommendation);
        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(recommendation);
    }

    public async Task<List<WeeklyRecommendationResponse>> GetCurrentWeekRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var weekStart = GetWeekStart(now);
        var weekEnd = weekStart.AddDays(6);

        var recommendations = await _context.WeeklyRecommendations
            .AsNoTracking()
            .Where(item => !item.IsDeleted
                && item.WeekStartDate == weekStart
                && item.WeekEndDate == weekEnd)
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync(cancellationToken);

        return recommendations.Select(ToResponse).ToList();
    }

    private static WeeklyRecommendationResponse ToResponse(WeeklyRecommendation item)
    {
        return new WeeklyRecommendationResponse(
            item.Id,
            item.BookTitle,
            item.Idea,
            item.AuthorUserId,
            item.AuthorName,
            item.CreatedDate,
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
}
