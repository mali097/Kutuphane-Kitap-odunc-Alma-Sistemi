using System.Net.Http.Json;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService()
    {
        _httpClient = ApiClientHelper.CreateClient();
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login",
                new { Email = username, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (login is null)
            {
                return null;
            }

            return new User
            {
                Id = login.UserId,
                UserId = login.UserId,
                Username = login.Email,
                FullName = $"{login.FirstName} {login.LastName}".Trim(),
                Role = login.Role,
                Token = login.Token
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RegisterAsync(User user)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/users/register", user);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
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
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            return await _httpClient.GetFromJsonAsync<List<User>>("/api/admin/users") ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.DeleteAsync($"/api/admin/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private sealed class LoginResponseDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
