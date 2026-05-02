using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Contracts;

namespace LibrarySystem.Api.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> AddAsync(CreateUserRequest request, int? actorUserId = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request, int? actorUserId = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, int? actorUserId = null, CancellationToken cancellationToken = default);
}
