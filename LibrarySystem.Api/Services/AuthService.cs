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
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var passwordHash = ComputeSha256(request.Password);

        var user = await _context.Users
            .FirstOrDefaultAsync(
                item => !item.IsDeleted
                    && item.Email.ToLowerInvariant() == normalizedEmail
                    && item.PasswordHash == passwordHash,
                cancellationToken);

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

    public bool TryGetUserIdByToken(string token, out int userId)
    {
        return SessionStore.TryGetValue(token.Trim(), out userId);
    }

    public static string ComputeSha256(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value.Trim());
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
