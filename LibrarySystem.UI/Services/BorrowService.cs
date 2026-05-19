using System.Net.Http.Json;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class BorrowService : IBorrowService
{
    private readonly HttpClient _httpClient;

    public BorrowService()
    {
        _httpClient = ApiClientHelper.CreateClient();
    }

    public async Task<List<BorrowRecord>> GetAllBorrowsAsync()
    {
        try { return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/borrows") ?? new(); }
        catch { return new(); }
    }

    public async Task<List<BorrowRecord>> GetUserBorrowsAsync(int userId)
    {
        try { return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>($"/api/borrows/user/{userId}") ?? new(); }
        catch { return new(); }
    }

    public async Task<List<BorrowRecord>> GetOverdueBorrowsAsync()
    {
        try { return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/borrows/overdue") ?? new(); }
        catch { return new(); }
    }

    public async Task<bool> BorrowBookAsync(int bookId, int userId, DateTime dueDate)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/borrows",
                new { BookId = bookId, UserId = userId, DueDate = dueDate });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ReturnBookAsync(int borrowId)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/borrows/return/{borrowId}", new { });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}