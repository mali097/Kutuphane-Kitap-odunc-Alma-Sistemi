namespace LibrarySystem.UI.Models;

public sealed class RateBookResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal MyRating { get; init; }
    public decimal? AverageRating { get; init; }
    public int RatingCount { get; init; }
}
