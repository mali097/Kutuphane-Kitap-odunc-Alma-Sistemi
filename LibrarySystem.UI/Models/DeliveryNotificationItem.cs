namespace LibrarySystem.UI.Models;

/// <summary>Ödünç teslim / iade bildirim satırı.</summary>
public sealed class DeliveryNotificationItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string RelativeTimeText { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "📘";
    public Color IconBackground { get; init; } = Color.FromArgb("#5E4BB6");
    public Color StatusDotColor { get; set; } = Color.FromArgb("#3B7CFF");
    public bool IsRead { get; set; }
}
