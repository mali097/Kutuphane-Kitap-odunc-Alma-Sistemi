using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class BookService : IBookService
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public BookService()
    {
        _httpClient = ApiClientHelper.CreateClient();
    }

    public async Task<List<Book>> GetAllBooksAsync()
        => await FetchBooksAsync("/api/books");

    public async Task<List<Book>> GetBooksByAuthorAsync(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            return await GetAllBooksAsync();
        }

        return await FetchBooksAsync($"/api/books?author={Uri.EscapeDataString(author.Trim())}");
    }

    public async Task<List<Book>> SearchBooksAsync(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await GetAllBooksAsync();
        }

        return await FetchBooksAsync($"/api/books?search={Uri.EscapeDataString(search.Trim())}");
    }

    private async Task<List<Book>> FetchBooksAsync(string url)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LastFetchError = $"API yanıtı: {(int)response.StatusCode}";
                return SessionHelper.CurrentUser is not null ? new() : GetLocalFallbackBooks();
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiBooks = JsonSerializer.Deserialize<List<ApiBookDto>>(json, ApiJsonOptions);
            LastFetchError = null;
            return apiBooks?.Select(MapFromApi).ToList() ?? new();
        }
        catch (Exception ex)
        {
            LastFetchError = ex.Message;
            if (SessionHelper.CurrentUser is not null)
            {
                return new();
            }
        }

        return GetLocalFallbackBooks();
    }

    public static string? LastFetchError { get; private set; }

    public Task<bool> AddBookAsync(Book book) => Task.FromResult(true);

    public Task<bool> UpdateBookAsync(Book book) => Task.FromResult(true);

    public async Task<List<FavoriteBook>> GetFavoritesAsync(int userId)
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.CurrentUser.Id != userId)
        {
            return new();
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var items = await _httpClient.GetFromJsonAsync<List<ApiFavoriteDto>>("/api/users/me/favorites");
            if (items is null || items.Count == 0)
            {
                return new();
            }

            return items.Select(MapFavorite).ToList();
        }
        catch
        {
            return new();
        }
    }

    public async Task<bool> AddFavoriteAsync(int userId, int bookId)
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.CurrentUser.Id != userId)
        {
            return false;
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.PostAsync($"/api/users/me/favorites/{bookId}", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveFavoriteAsync(int userId, int bookId)
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.CurrentUser.Id != userId)
        {
            return false;
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.DeleteAsync($"/api/users/me/favorites/{bookId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> DeleteBookAsync(int bookId)
        => Task.FromResult(true);

    private static Book MapFromApi(ApiBookDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title ?? "",
        Author = dto.Author ?? "",
        ISBN = dto.Isbn ?? "",
        Category = dto.Genres is { Count: > 0 }
            ? string.Join(", ", dto.Genres)
            : string.IsNullOrWhiteSpace(dto.Genre) ? dto.Category ?? "" : dto.Genre,
        PublishYear = dto.PublishYear,
        PageCount = dto.PageCount,
        Publisher = dto.Publisher ?? "",
        Description = dto.Description ?? "",
        CoverImageUrl = ApiClientHelper.GetBookCoverUrl(dto.Id),
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

    private static FavoriteBook MapFavorite(ApiFavoriteDto dto) => new()
    {
        BookId = dto.BookId,
        UserId = SessionHelper.CurrentUser?.Id ?? 0,
        BookTitle = dto.Title ?? string.Empty,
        BookAuthor = dto.Author ?? string.Empty,
        BookCategory = dto.Genres is { Count: > 0 } ? string.Join(", ", dto.Genres) : string.Empty,
        BookPublisher = dto.Publisher ?? string.Empty,
        BookPageCount = dto.PageCount,
        AddedAt = dto.FavoritedAt
    };

    private sealed class ApiFavoriteDto
    {
        [JsonPropertyName("bookId")]
        public int BookId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("pageCount")]
        public int PageCount { get; set; }

        [JsonPropertyName("favoritedAt")]
        public DateTime FavoritedAt { get; set; }
    }

    private sealed class ApiBookDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Isbn { get; set; }
        public string? Genre { get; set; }
        public List<string>? Genres { get; set; }
        public string? Category { get; set; }
        public int PublishYear { get; set; }
        public int PageCount { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; } = true;
        public decimal? AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}
