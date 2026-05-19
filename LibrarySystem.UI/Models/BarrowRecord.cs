namespace LibrarySystem.UI.Models;

public class BorrowRecord
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsReturned { get; set; }

    // Hesaplanan alanlar
    public int DaysOverdue => !IsReturned && DateTime.Now > DueDate
        ? (int)(DateTime.Now - DueDate).TotalDays : 0;

    public double Fine => DaysOverdue * 1.5; // günlük 1.5 TL ceza

    public string Status => IsReturned ? "✅ İade Edildi"
        : DaysOverdue > 0 ? $"⚠️ {DaysOverdue} gün gecikmiş"
        : "📖 Ödünçte";
}