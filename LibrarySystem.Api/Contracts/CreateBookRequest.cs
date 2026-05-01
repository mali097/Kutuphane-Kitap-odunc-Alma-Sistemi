namespace LibrarySystem.Api.Contracts;

public sealed class CreateBookRequest
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public string Genre { get; init; } = string.Empty;
    public int PublishYear { get; init; }
    public bool IsAvailable { get; init; } = true;
}
