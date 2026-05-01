namespace LibrarySystem.Api.Contracts;

public sealed class GetBooksQuery
{
    public string? Search { get; init; }
    public string? Genre { get; init; }
    public bool? IsAvailable { get; init; }
    public int? PublishYear { get; init; }
}
