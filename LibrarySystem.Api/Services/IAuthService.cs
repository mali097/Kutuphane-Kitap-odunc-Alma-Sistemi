using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IAuthService
{
    Task<UserLoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(UserLogoutRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
