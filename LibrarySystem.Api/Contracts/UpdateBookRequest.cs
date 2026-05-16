namespace LibrarySystem.Api.Contracts;

public sealed class UpdateBookRequest
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    /// <summary>Genre names, e.g. "polisiye", "fantastik" (not numeric ids).</summary>
    public List<string>? Genres { get; init; }
    public int? PublishYear { get; init; }
    public bool? IsAvailable { get; init; }
}
