using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> RegisterAsync(User user);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(int userId);
}