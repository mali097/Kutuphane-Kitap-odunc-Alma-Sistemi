using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly LibraryDbContext _context;
    private static readonly ConcurrentDictionary<string, int> SessionStore = new();

    public AuthService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<UserLoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        return await LoginInternalAsync(request, requireAdmin: false, cancellationToken);
    }

    public async Task<UserLoginResponse?> LoginAdminAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await LoginInternalAsync(request, requireAdmin: true, cancellationToken);
        if (response is null)
        {
            return null;
        }

        if (!string.Equals(response.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            SessionStore.TryRemove(response.Token.Trim(), out _);
            return null;
        }

        return response;
    }

    private async Task<UserLoginResponse?> LoginInternalAsync(
        LoginRequest request,
        bool requireAdmin,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var passwordHash = ComputeSha256(request.Password);

        IQueryable<User> query = _context.Users
            .Where(item => !item.IsDeleted
                && item.Email == normalizedEmail
                && item.PasswordHash == passwordHash);

        if (requireAdmin)
        {
            query = query.Where(item => item.Role == "Admin");
        }

        var user = await query.FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var token = Guid.NewGuid().ToString("N");
        SessionStore[token] = user.Id;
        user.UpdatedBy = user.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return new UserLoginResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            token
        );
    }

    public Task<bool> LogoutAsync(UserLogoutRequest request, CancellationToken cancellationToken = default)
    {
        var sessionToken = request.SessionToken.Trim();
        var removed = SessionStore.TryRemove(sessionToken, out var userId);
        if (!removed)
        {
            return Task.FromResult(false);
        }

        var user = _context.Users.FirstOrDefault(item => item.Id == userId && !item.IsDeleted);
        if (user is not null)
        {
            user.UpdatedBy = userId;
            _context.SaveChanges();
        }

        return Task.FromResult(removed);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken);

        if (user is null)
        {
            return false;
        }

        var oldHash = ComputeSha256(request.CurrentPassword);
        if (!string.Equals(user.PasswordHash, oldHash, StringComparison.Ordinal))
        {
            return false;
        }

        user.PasswordHash = ComputeSha256(request.NewPassword);
        user.UpdatedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int?> GetUserIdBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var token = sessionToken.Trim();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (!SessionStore.TryGetValue(token, out var userId))
        {
            return null;
        }

        var userExists = await _context.Users
            .AnyAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken);

        return userExists ? userId : null;
    }

    public async Task<bool> IsUserAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await IsUserInRoleAsync(userId, "Admin", cancellationToken);
    }

    public async Task<bool> IsUserAuthorAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await IsUserInRoleAsync(userId, "Author", cancellationToken);
    }

    private async Task<bool> IsUserInRoleAsync(int userId, string role, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(item => item.Id == userId && !item.IsDeleted && item.Role == role, cancellationToken);
    }

    public static string ComputeSha256(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value.Trim());
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
