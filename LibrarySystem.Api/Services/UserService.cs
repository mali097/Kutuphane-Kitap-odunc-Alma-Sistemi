using LibrarySystem.Api.Data;
using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public sealed class UserService : IUserService
{
    private readonly LibraryDbContext _context;

    public UserService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AdminExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(user => !user.IsDeleted && user.Role == "Admin", cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted, cancellationToken);
    }

    public async Task<int> AddAsync(CreateUserRequest request, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var newUser = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = AuthService.ComputeSha256(request.PasswordHash),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role.Trim(),
            CreatedBy = actorUserId ?? 0
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);
        return newUser.Id;
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (HasMeaningfulValue(request.FirstName))
        {
            user.FirstName = request.FirstName!.Trim();
        }

        if (HasMeaningfulValue(request.LastName))
        {
            user.LastName = request.LastName!.Trim();
        }

        if (HasMeaningfulValue(request.Email))
        {
            user.Email = request.Email!.Trim();
        }

        if (HasMeaningfulValue(request.Role))
        {
            user.Role = request.Role!.Trim();
        }

        if (HasMeaningfulValue(request.PasswordHash))
        {
            user.PasswordHash = AuthService.ComputeSha256(request.PasswordHash!);
        }

        user.UpdatedBy = actorUserId ?? user.UpdatedBy ?? 0;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.UpdatedBy = actorUserId ?? user.UpdatedBy ?? 0;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool HasMeaningfulValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value.Trim(), "string", StringComparison.OrdinalIgnoreCase);
    }
}
