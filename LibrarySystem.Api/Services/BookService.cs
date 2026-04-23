using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;

namespace LibrarySystem.Api.Services
{
    // 21. madde: Service sınıfı oluşturma
    public class BookService : IBookService
    {
        private readonly LibraryDbContext _context;

        public BookService(LibraryDbContext context)
        {
            _context = context;
        }

        public List<Book> GetAllBooks()
        {
            // Veritabanındaki tüm kitapları listele (Madde 7 ve 24)
            return _context.Books.Where(x => !x.IsDeleted).ToList();
        }

        public int AddBook(Book newBook)
        {
            // Veritabanına ekle (Madde 8)
            _context.Books.Add(newBook);
            _context.SaveChanges();
            return newBook.Id;
        }

        public bool DeleteBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null) return false;

            // Soft Delete (Madde 10)
            book.IsDeleted = true;
            _context.SaveChanges();
            return true;
        }
    }
}
