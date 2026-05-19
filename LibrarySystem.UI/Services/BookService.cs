using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class BookService : IBookService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://10.0.2.2:5000";

    public BookService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async Task<List<Book>> GetAllBooksAsync()
    {
        try
        {
            var apiBooks = await _httpClient.GetFromJsonAsync<List<ApiBookDto>>("/api/books");
            if (apiBooks is { Count: > 0 })
                return apiBooks.Select(MapFromApi).ToList();
        }
        catch
        {
            // API henüz yoksa veya erişilemiyorsa yerel liste kullanılır
        }

        return GetLocalFallbackBooks();
    }

    public Task<bool> AddBookAsync(Book book) => Task.FromResult(true);

    public Task<bool> UpdateBookAsync(Book book) => Task.FromResult(true);

    public Task<List<FavoriteBook>> GetFavoritesAsync(int userId)
        => Task.FromResult(new List<FavoriteBook>());

    public Task<bool> AddFavoriteAsync(int userId, int bookId)
        => Task.FromResult(true);

    public Task<bool> RemoveFavoriteAsync(int userId, int bookId)
        => Task.FromResult(true);

    public Task<bool> DeleteBookAsync(int bookId)
        => Task.FromResult(true);

    private static Book MapFromApi(ApiBookDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title ?? "",
        Author = dto.Author ?? "",
        ISBN = dto.Isbn ?? "",
        Category = string.IsNullOrWhiteSpace(dto.Genre) ? dto.Category ?? "" : dto.Genre,
        PublishYear = dto.PublishYear,
        PageCount = dto.PageCount,
        Publisher = dto.Publisher ?? "",
        Description = dto.Description ?? "",
        IsAvailable = dto.IsAvailable
    };

    private static List<Book> GetLocalFallbackBooks() =>
    [
        new() { Id = 1, Title = "1984", Author = "George Orwell", ISBN = "9789750707104", Category = "Bilimkurgu", PublishYear = 1949, PageCount = 328, Publisher = "Can Yayınları", Description = "Totaliter bir dünyada geçen distopik bir klasik.", IsAvailable = true },
        new() { Id = 2, Title = "Suç ve Ceza", Author = "Fyodor Dostoyevski", ISBN = "9789750719107", Category = "Roman", PublishYear = 1866, PageCount = 671, Publisher = "İş Bankası Kültür", Description = "Raskolnikov'un vicdan ve suç üzerine derin yolculuğu.", IsAvailable = true },
        new() { Id = 3, Title = "Kürk Mantolu Madonna", Author = "Sabahattin Ali", ISBN = "9789750719381", Category = "Türk Klasikleri", PublishYear = 1943, PageCount = 160, Publisher = "Yapı Kredi", Description = "Aşk ve yalnızlığın Türk edebiyatındaki unutulmaz hikâyesi.", IsAvailable = true },
        new() { Id = 4, Title = "Simyacı", Author = "Paulo Coelho", ISBN = "9789750719503", Category = "Kişisel Gelişim", PublishYear = 1988, PageCount = 184, Publisher = "Can Yayınları", Description = "Kişisel efsane ve kader üzerine sembolik bir yolculuk.", IsAvailable = true },
        new() { Id = 5, Title = "Kara Kitap", Author = "Orhan Pamuk", ISBN = "9789750719121", Category = "Polisiye", PublishYear = 1990, PageCount = 448, Publisher = "İletişim", Description = "İstanbul'da geçen gizemli ve katmanlı bir polisiye.", IsAvailable = true }
    ];

    private sealed class ApiBookDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Isbn { get; set; }
        public string? Genre { get; set; }
        public string? Category { get; set; }
        public int PublishYear { get; set; }
        public int PageCount { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; } = true;

        [JsonPropertyName("pageCount")]
        public int PageCountAlt { set => PageCount = value; }
    }
}
