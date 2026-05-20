using System.Net.Http.Json;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public sealed class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;

    public NotificationService()
    {
        _httpClient = ApiClientHelper.CreateClient();
    }

    public async Task<List<DeliveryNotificationItem>> GetUserNotificationsAsync(int userId)
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.CurrentUser.Id != userId)
        {
            return new();
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var items = await _httpClient.GetFromJsonAsync<List<ApiNotificationDto>>("/api/users/me/notifications");
            if (items is null || items.Count == 0)
            {
                return new();
            }

            return items.Select(MapToItem).OrderByDescending(item => item.OccurredAt).ToList();
        }
        catch
        {
            return new();
        }
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            await _httpClient.PatchAsync($"/api/notifications/{notificationId}/read", null);
        }
        catch
        {
            // ignore
        }
    }

    private static DeliveryNotificationItem MapToItem(ApiNotificationDto dto)
    {
        var type = dto.NotificationType ?? string.Empty;
        var (glyph, bg, dot) = type switch
        {
            "ReturnedOnTime" => ("✅", "#2EA77E", "#2EA77E"),
            "ReturnedLate" => ("⚠️", "#C62828", "#C62828"),
            "DueDateReminder" => ("⏰", "#F29B3A", "#F29B3A"),
            _ => ("📗", "#5E4BB6", "#3B7CFF")
        };

        return new DeliveryNotificationItem
        {
            Id = dto.Id.ToString(),
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "Bildirim" : dto.Title,
            Detail = dto.Message ?? string.Empty,
            OccurredAt = dto.OccurredAt,
            RelativeTimeText = ToRelativeTr(dto.OccurredAt),
            IconGlyph = glyph,
            IconBackground = Color.FromArgb(bg),
            StatusDotColor = dto.IsRead ? Color.FromArgb("#C5C2CC") : Color.FromArgb(dot),
            IsRead = dto.IsRead
        };
    }

    private static string ToRelativeTr(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var span = DateTime.Now - local;
        if (span.TotalSeconds < 45) return "Az önce";
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)} dk önce";
        if (span.TotalHours < 24) return $"{Math.Max(1, (int)span.TotalHours)} saat önce";
        if (span.TotalDays < 7) return $"{Math.Max(1, (int)span.TotalDays)} gün önce";
        return local.ToString("dd.MM.yyyy");
    }

    private sealed class ApiNotificationDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? NotificationType { get; set; }
        public bool IsRead { get; set; }
    }
}
