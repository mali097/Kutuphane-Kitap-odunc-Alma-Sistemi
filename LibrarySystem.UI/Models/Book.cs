namespace LibrarySystem.UI.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public int PageCount { get; set; }
    public int PublishYear { get; set; }

    public string Publisher { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
