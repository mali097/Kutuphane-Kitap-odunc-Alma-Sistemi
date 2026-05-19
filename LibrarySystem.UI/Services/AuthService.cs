using System.Net.Http.Json;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    // ⚠️ Telefonda test ederken buraya bilgisayarının IP'sini yaz (cmd → ipconfig)
    private const string BaseUrl = "http://10.0.2.2:5000"; // Android emulator için
    // Gerçek telefon için: "http://192.168.1.XXX:5000"

    public AuthService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login",
                new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<User>();
            return null;
        }
        catch { return null; }
    }

    public async Task<bool> RegisterAsync(User user)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", user);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/auth/change-password/{userId}",
                new { OldPassword = oldPassword, NewPassword = newPassword });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<User>>("/api/users") ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}