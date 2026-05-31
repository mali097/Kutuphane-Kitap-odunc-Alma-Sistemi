using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, string? expectedRole = null);
    Task<User?> GetMyProfileAsync();
    Task<bool> RegisterAsync(string firstName, string lastName, string email, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(int userId);
}