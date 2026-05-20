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
        if (!SessionHelper.IsAdmin)
        {
            return new();
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/borrows") ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<BorrowRecord>> GetUserBorrowsAsync(int userId)
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.CurrentUser.Id != userId)
        {
            return new();
        }

        return await GetMyBorrowsAsync();
    }

    public async Task<List<BorrowRecord>> GetMyBorrowsAsync()
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/users/me/borrows") ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<BorrowRecord>> GetMyActiveBorrowsAsync()
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.IsAdmin)
        {
            return new();
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/users/me/borrows/active") ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<BorrowRecord>> GetOverdueBorrowsAsync()
    {
        if (!SessionHelper.IsAdmin)
        {
            return new();
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            return await _httpClient.GetFromJsonAsync<List<BorrowRecord>>("/api/borrows/overdue") ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> BorrowBookAsync(int bookId, int userId)
    {
        var currentUser = SessionHelper.CurrentUser;
        if (currentUser is null)
        {
            return false;
        }

        if (!SessionHelper.IsAdmin && currentUser.Id != userId)
        {
            return false;
        }

        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.PostAsJsonAsync("/api/borrows",
                new { BookId = bookId, UserId = currentUser.Id });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ReturnBookAsync(int borrowId)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.PutAsync($"/api/borrows/return/{borrowId}", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
