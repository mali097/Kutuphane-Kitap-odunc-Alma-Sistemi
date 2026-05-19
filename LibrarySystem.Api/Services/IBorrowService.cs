using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Services;

public interface IBorrowService
{
    Task<int?> BorrowBookAsync(BorrowBookRequest request, int actingUserId, CancellationToken cancellationToken = default);
    Task<bool> ReturnBookAsync(ReturnBookRequest request, int actingUserId, CancellationToken cancellationToken = default);
    Task<List<BorrowRecord>> GetBorrowRecordsAsync(
        int? userId,
        int? bookId,
        bool? isReturned,
        CancellationToken cancellationToken = default);
    Task<List<BorrowRecord>> GetAllBorrowRecordsWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<List<BorrowRecord>> GetActiveBorrowRecordsWithDetailsAsync(CancellationToken cancellationToken = default);
}
