namespace LibrarySystem.UI.Models;

public sealed class SupportHelpCategory
{
    public string Id { get; init; } = string.Empty;
    public string Icon { get; init; } = "📚";
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public IReadOnlyList<SupportFaqItem> Faqs { get; init; } = [];
}
