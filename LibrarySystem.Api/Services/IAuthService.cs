using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IAuthService
{
    Task<UserLoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserLoginResponse?> LoginAdminAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(UserLogoutRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<int?> GetUserIdBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<bool> IsUserAdminAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserAuthorAsync(int userId, CancellationToken cancellationToken = default);
}
