using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

    public async Task<LoginResult> LoginAsync(string email, string password, string? expectedRole = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login",
                new { Email = email.Trim(), Password = password });

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return LoginResult.Fail("E-posta veya şifre hatalı.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return LoginResult.Fail("Giriş yapılamadı. Lütfen tekrar deneyin.");
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (login is null || login.UserId <= 0 || string.IsNullOrWhiteSpace(login.Token))
            {
                return LoginResult.Fail("Sunucu yanıtı okunamadı.");
            }

            if (!string.IsNullOrWhiteSpace(expectedRole)
                && !RolesMatch(login.Role, expectedRole))
            {
                return LoginResult.Fail(GetRoleMismatchMessage(expectedRole));
            }

            var trimmedEmail = email.Trim();
            var username = GetUsernameFromEmail(trimmedEmail);

            return LoginResult.Ok(new User
            {
                Id = login.UserId,
                UserId = login.UserId,
                Username = username,
                Email = trimmedEmail,
                FullName = $"{login.FirstName} {login.LastName}".Trim(),
                Role = login.Role,
                Token = login.Token,
                CreatedAt = login.CreatedDate
            });
        }
        catch (HttpRequestException)
        {
            return LoginResult.Fail(
                "API'ye bağlanılamadı. Önce LibrarySystem.Api projesini çalıştırın (http://localhost:5279).");
        }
        catch (TaskCanceledException)
        {
            return LoginResult.Fail("İstek zaman aşımına uğradı. API çalışıyor mu kontrol edin.");
        }
        catch
        {
            return LoginResult.Fail("Bağlantı hatası oluştu.");
        }
    }

    public async Task<User?> GetMyProfileAsync()
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var profile = await _httpClient.GetFromJsonAsync<UserProfileDto>("/api/users/me");
            if (profile is null || profile.Id <= 0)
            {
                return null;
            }

            return new User
            {
                Id = profile.Id,
                UserId = profile.Id,
                Email = profile.Email ?? string.Empty,
                FullName = $"{profile.FirstName} {profile.LastName}".Trim(),
                Role = profile.Role,
                Username = GetUsernameFromEmail(profile.Email ?? string.Empty),
                Token = SessionHelper.CurrentUser?.Token ?? string.Empty,
                CreatedAt = profile.CreatedDate
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/users/register", new
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email.Trim(),
                PasswordHash = password,
                Role = "Student"
            });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            ApiClientHelper.ApplySessionHeaders(_httpClient);
            var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", new
            {
                CurrentPassword = oldPassword,
                NewPassword = newPassword
            });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
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

    private static bool RolesMatch(string actualRole, string expectedRole)
    {
        if (string.Equals(expectedRole, "Author", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(actualRole, "Author", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(expectedRole, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(actualRole, "Student", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actualRole, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(actualRole, expectedRole, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetUsernameFromEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    private static string GetRoleMismatchMessage(string expectedRole)
    {
        if (string.Equals(expectedRole, "Author", StringComparison.OrdinalIgnoreCase))
        {
            return "Bu hesap yazar hesabı değil. Lütfen Öğrenci girişi sekmesini kullanın.";
        }

        return "Bu hesap kullanıcı hesabı değil. Lütfen Yazar girişi sekmesini kullanın.";
    }

    private sealed class LoginResponseDto
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }
    }

    private sealed class UserProfileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }
    }
}
