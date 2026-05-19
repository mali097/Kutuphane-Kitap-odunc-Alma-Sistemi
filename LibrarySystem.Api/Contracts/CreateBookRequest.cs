namespace LibrarySystem.Api.Contracts;

public sealed class CreateBookRequest
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    /// <summary>Genre names, e.g. "polisiye", "fantastik" (not numeric ids).</summary>
    public List<string> Genres { get; init; } = [];
    public int PublishYear { get; init; }
    public bool IsAvailable { get; init; } = true;
}
