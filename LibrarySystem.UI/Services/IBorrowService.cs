using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Services;

public interface IBorrowService
{
    Task<List<BorrowRecord>> GetAllBorrowsAsync();
    Task<List<BorrowRecord>> GetUserBorrowsAsync(int userId);
    Task<List<BorrowRecord>> GetMyBorrowsAsync();
    Task<List<BorrowRecord>> GetMyActiveBorrowsAsync();
    Task<List<BorrowRecord>> GetOverdueBorrowsAsync();
    Task<bool> BorrowBookAsync(int bookId, int userId);
    Task<bool> ReturnBookAsync(int borrowId);
}
