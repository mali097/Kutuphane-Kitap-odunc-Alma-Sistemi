using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Services
{
    
    public interface IBookService
    {
        List<Book> GetAllBooks();
        int AddBook(Book newBook);
        bool DeleteBook(int id);
    }
}