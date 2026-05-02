namespace LibrarySystem.Api.Contracts;

public sealed class UpdateBookRequest
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Isbn { get; init; }
    public string? Genre { get; init; }
    public int? PublishYear { get; init; }
    public bool? IsAvailable { get; init; }
}
