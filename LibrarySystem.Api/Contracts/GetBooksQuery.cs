namespace LibrarySystem.Api.Contracts;

public sealed class GetBooksQuery
{
    /// <summary>Kitap adı, yazar veya ISBN ile arar (tek arama kutusu).</summary>
    public string? Search { get; init; }
    /// <summary>Yalnızca yazar adına göre filtreler.</summary>
    public string? Author { get; init; }
    public string? Genre { get; init; }
    public bool? IsAvailable { get; init; }
    public int? PublishYear { get; init; }
}
