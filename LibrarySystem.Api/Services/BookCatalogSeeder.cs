using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Api.Services;

public static class BookCatalogSeeder
{
    public const int BooksPerCategory = 3;

    public static async Task SeedAsync(LibraryDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        var existingBooks = await context.Books
            .Where(book => !book.IsDeleted)
            .ToListAsync(cancellationToken);

        var addedCount = 0;

        foreach (var genre in Enum.GetValues<GenreType>())
        {
            var currentCount = existingBooks.Count(book => book.Genres.Contains(genre));
            if (currentCount >= BooksPerCategory)
            {
                continue;
            }

            var needed = BooksPerCategory - currentCount;
            var candidates = GetSeedBooks(genre);

            foreach (var seed in candidates)
            {
                if (needed <= 0)
                {
                    break;
                }

                var alreadyExists = existingBooks.Any(book =>
                    string.Equals(book.Title, seed.Title, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(book.Author, seed.Author, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    continue;
                }

                var book = new Book
                {
                    Title = seed.Title,
                    Author = seed.Author,
                    Genres = [genre],
                    PublishYear = seed.PublishYear,
                    Publisher = seed.Publisher,
                    PageCount = seed.PageCount,
                    IsAvailable = true,
                    CreatedBy = 0
                };

                context.Books.Add(book);
                await context.SaveChangesAsync(cancellationToken);

                book.Isbn = IsbnGenerator.GenerateForBookId(book.Id);
                await context.SaveChangesAsync(cancellationToken);

                existingBooks.Add(book);
                addedCount++;
                needed--;
            }
        }

        if (addedCount > 0)
        {
            logger.LogInformation("Book catalog seed completed. {AddedCount} book(s) added.", addedCount);
        }
    }

    private static IReadOnlyList<SeedBookDefinition> GetSeedBooks(GenreType genre) => genre switch
    {
        GenreType.polisiye =>
        [
            new("Doğu Ekspresinde Cinayet", "Agatha Christie", 1934, "Altın Kitaplar", 256),
            new("Beyoğlu Rapsodisi", "Ahmet Ümit", 1993, "Everest Yayınları", 384),
            new("Kara Kitap", "Orhan Pamuk", 1990, "İletişim Yayınları", 448)
        ],
        GenreType.bilimkurgu =>
        [
            new("Vakıf", "Isaac Asimov", 1951, "İthaki Yayınları", 320),
            new("Dune", "Frank Herbert", 1965, "İthaki Yayınları", 688),
            new("1984", "George Orwell", 1949, "Can Yayınları", 328)
        ],
        GenreType.fantastik =>
        [
            new("Yüzüklerin Efendisi", "J.R.R. Tolkien", 1954, "Metis Yayınları", 1216),
            new("Harry Potter ve Felsefe Taşı", "J.K. Rowling", 1997, "Yapı Kredi Yayınları", 296),
            new("Aslan, Cadı ve Dolap", "C.S. Lewis", 1950, "Doğan Egmont", 208)
        ],
        GenreType.tarih =>
        [
            new("Nutuk", "Mustafa Kemal Atatürk", 1927, "Türkiye İş Bankası Kültür", 900),
            new("Sapiens: İnsan Türünün Kısa Tarihi", "Yuval Noah Harari", 2011, "Kolektif Kitap", 448),
            new("Osmanlı İmparatorluğu", "Halil İnalcık", 2003, "Yapı Kredi Yayınları", 320)
        ],
        GenreType.biyografi =>
        [
            new("Steve Jobs", "Walter Isaacson", 2011, "Pegasus Yayınları", 656),
            new("Einstein: Yaşamı ve Evreni", "Walter Isaacson", 2007, "Domingo Yayınevi", 704),
            new("Atatürk", "Andrew Mango", 1999, "Remzi Kitabevi", 704)
        ],
        GenreType.cocuk =>
        [
            new("Pinokyo", "Carlo Collodi", 1883, "Tudem Yayınları", 192),
            new("Kırmızı Başlıklı Kız", "Grimm Kardeşler", 1812, "Tudem Yayınları", 32),
            new("Momo", "Michael Ende", 1973, "Can Çocuk Yayınları", 240)
        ],
        GenreType.macera =>
        [
            new("Robinson Crusoe", "Daniel Defoe", 1719, "İş Bankası Kültür", 320),
            new("Define Adası", "Robert Louis Stevenson", 1883, "İş Bankası Kültür", 304),
            new("Tom Sawyer'ın Maceraları", "Mark Twain", 1876, "Türkiye İş Bankası Kültür", 272)
        ],
        GenreType.dram =>
        [
            new("Anna Karenina", "Lev Tolstoy", 1877, "İş Bankası Kültür", 864),
            new("Sefiller", "Victor Hugo", 1862, "İş Bankası Kültür", 1232),
            new("Baba ve Piç", "Elif Shafak", 2006, "Metis Yayınları", 480)
        ],
        GenreType.korku =>
        [
            new("Dracula", "Bram Stoker", 1897, "İthaki Yayınları", 488),
            new("Göz", "Stephen King", 1986, "Pegasus Yayınları", 384),
            new("Frankenstein", "Mary Shelley", 1818, "İthaki Yayınları", 256)
        ],
        GenreType.hikaye =>
        [
            new("Medcezir", "Sabahattin Ali", 1935, "Yapı Kredi Yayınları", 96),
            new("Öyküler", "Sait Faik Abasıyanık", 1940, "Yapı Kredi Yayınları", 224),
            new("İstanbul Hatırası", "Orhan Pamuk", 2003, "Yapı Kredi Yayınları", 144)
        ],
        GenreType.siir =>
        [
            new("Safahat", "Mehmet Akif Ersoy", 1911, "Erkam Matbaası", 512),
            new("Sevgilim İstanbul", "Nazım Hikmet", 1966, "Yapı Kredi Yayınları", 128),
            new("Bütün Şiirleri", "Cemal Süreya", 1991, "Yapı Kredi Yayınları", 640)
        ],
        GenreType.kisisel_gelisim =>
        [
            new("Simyacı", "Paulo Coelho", 1988, "Can Yayınları", 184),
            new("Atomik Alışkanlıklar", "James Clear", 2018, "Penguin Random House", 320),
            new("İkna Psikolojisi", "Robert Cialdini", 1984, "MediaCat Yayınları", 512)
        ],
        GenreType.psikoloji =>
        [
            new("İnsan Olmak", "Erich Fromm", 1947, "Arkadaş Yayınları", 224),
            new("Düşünce Hızında", "Daniel Kahneman", 2011, "Pegasus Yayınları", 512),
            new("Psikolojiye Giriş", "Wayne Weiten", 2014, "Kaknüs Yayınları", 768)
        ],
        GenreType.sanat =>
        [
            new("Sanatın Gizli Tarihi", "E.H. Gombrich", 1950, "Remzi Kitabevi", 512),
            new("Bir Baskının Portresi", "Orhan Pamuk", 2008, "İletişim Yayınları", 224),
            new("Resim Sanatının Tarihi", "E.H. Gombrich", 1950, "Remzi Kitabevi", 688)
        ],
        GenreType.felsefe =>
        [
            new("Sofie'nin Dünyası", "Jostein Gaarder", 1991, "Pan Yayıncılık", 592),
            new("Devlet", "Platon", -380, "İş Bankası Kültür", 512),
            new("Böyle Buyurdu Zerdüşt", "Friedrich Nietzsche", 1883, "İş Bankası Kültür", 352)
        ],
        GenreType.bilim =>
        [
            new("Kısa Zamanın Tarihi", "Stephen Hawking", 1988, "Altın Kitaplar", 256),
            new("Kozmos", "Carl Sagan", 1980, "Altın Kitaplar", 384),
            new("Olasılıksızlığın Mimarisi", "Richard Dawkins", 1986, "Alfa Yayınları", 352)
        ],
        GenreType.ekonomi =>
        [
            new("Kapitalizmin 21. Yüzyılı", "Thomas Piketty", 2013, "Doğan Kitap", 768),
            new("Freakonomics", "Steven Levitt", 2005, "Doğan Kitap", 320),
            new("Zengin Baba Yoksul Baba", "Robert Kiyosaki", 1997, "Altın Kitaplar", 336)
        ],
        GenreType.hukuk =>
        [
            new("Anayasa Hukuku", "Ergun Özbudun", 2016, "Yetkin Yayınları", 512),
            new("Hukuk Felsefesi", "H.L.A. Hart", 1961, "Adalet Yayınevi", 352),
            new("Medeni Hukuk", "Turgut Akıntürk", 2018, "Beta Basım", 640)
        ],
        GenreType.din =>
        [
            new("Kur'an-ı Kerim Meali", "Elmalılı Hamdi Yazır", 1935, "Huzur Yayınevi", 704),
            new("İslam Tarihi", "Muhammad Husayn Haykal", 1935, "Dergah Yayınları", 512),
            new("Mevlana", "Tahsin Yazıcı", 1951, "MEB Yayınları", 128)
        ],
        GenreType.gezi =>
        [
            new("Seyahatname", "Evliya Çelebi", 1670, "Yapı Kredi Yayınları", 512),
            new("Gezgin", "Paul Theroux", 1975, "Pan Yayıncılık", 384),
            new("Türkiye'nin Uçları", "Aziz Nesin", 1975, "Evrensel Basım Yayın", 256)
        ],
        GenreType.dunya_klasikleri =>
        [
            new("Suç ve Ceza", "Fyodor Dostoyevski", 1866, "İş Bankası Kültür", 671),
            new("Savaş ve Barış", "Lev Tolstoy", 1869, "İş Bankası Kültür", 1216),
            new("Don Quijote", "Miguel de Cervantes", 1605, "İş Bankası Kültür", 1024)
        ],
        GenreType.turk_klasikleri =>
        [
            new("Çalıkuşu", "Reşat Nuri Güntekin", 1922, "İnkılap Kitabevi", 448),
            new("Kürk Mantolu Madonna", "Sabahattin Ali", 1943, "Yapı Kredi Yayınları", 160),
            new("Tutunamayanlar", "Oğuz Atay", 1972, "İletişim Yayınları", 724)
        ],
        _ => []
    };

    private sealed record SeedBookDefinition(
        string Title,
        string Author,
        int PublishYear,
        string Publisher,
        int PageCount);
}
