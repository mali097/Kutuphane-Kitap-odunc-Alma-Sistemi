using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
}
