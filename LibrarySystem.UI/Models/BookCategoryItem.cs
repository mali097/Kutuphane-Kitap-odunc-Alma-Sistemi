namespace LibrarySystem.UI.Models;

public class BookCategoryItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "📚";
    public Color IconBackground { get; init; } = Color.FromArgb("#E8E0F5");
    public int BookCount { get; set; }
    public string BookCountText => $"{BookCount} kitap";
}

public static class BookCategories
{
    public static IReadOnlyList<BookCategoryItem> All { get; } =
    [
        new() { Id = 1, Name = "Polisiye", Icon = "🔍", IconBackground = Color.FromArgb("#E3E8F5"), Description = "Suç, gizem ve dedektif hikâyeleriyle gerilimi dorukta yaşatan eserler." },
        new() { Id = 2, Name = "Bilimkurgu", Icon = "🚀", IconBackground = Color.FromArgb("#E0F0FF"), Description = "Gelecek, teknoloji ve alternatif dünyalarda geçen hayal gücü dolu romanlar." },
        new() { Id = 3, Name = "Fantastik", Icon = "🐉", IconBackground = Color.FromArgb("#F3E8FF"), Description = "Büyü, efsane ve olağanüstü varlıklarla dolu destansı maceralar." },
        new() { Id = 4, Name = "Tarih", Icon = "🏛️", IconBackground = Color.FromArgb("#FFF3E0"), Description = "Geçmişin izini süren, olayları ve dönemleri anlatan kitaplar." },
        new() { Id = 5, Name = "Biyografi", Icon = "👤", IconBackground = Color.FromArgb("#E8F5E9"), Description = "Gerçek insanların yaşam öykülerini ve ilham veren yolculuklarını anlatır." },
        new() { Id = 6, Name = "Çocuk", Icon = "🧸", IconBackground = Color.FromArgb("#FFF9C4"), Description = "Küçük okurlar için eğlenceli, öğretici ve hayal gücünü geliştiren kitaplar." },
        new() { Id = 7, Name = "Macera", Icon = "🧭", IconBackground = Color.FromArgb("#E1F5FE"), Description = "Heyecan dolu yolculuklar, keşifler ve sürükleyici aksiyon hikâyeleri." },
        new() { Id = 8, Name = "Dram", Icon = "🎭", IconBackground = Color.FromArgb("#FCE4EC"), Description = "İnsan ilişkileri, duygular ve yaşamın derinliklerini işleyen eserler." },
        new() { Id = 9, Name = "Korku", Icon = "👻", IconBackground = Color.FromArgb("#ECEFF1"), Description = "Gerilim ve korku unsurlarıyla okuru diken üstünde tutan hikâyeler." },
        new() { Id = 10, Name = "Hikaye", Icon = "📖", IconBackground = Color.FromArgb("#EFEBE9"), Description = "Kısa öykülerle farklı karakter ve dünyaları bir arada sunar." },
        new() { Id = 11, Name = "Şiir", Icon = "✒️", IconBackground = Color.FromArgb("#F3E5F5"), Description = "Duygu, ritim ve imgelerle yoğunlaştırılmış edebi ifadeler." },
        new() { Id = 12, Name = "Kişisel Gelişim", Icon = "📈", IconBackground = Color.FromArgb("#E8F5E9"), Description = "Motivasyon, alışkanlık ve kişisel dönüşüm üzerine rehber niteliğinde kitaplar." },
        new() { Id = 13, Name = "Psikoloji", Icon = "🧠", IconBackground = Color.FromArgb("#E3F2FD"), Description = "İnsan zihni, davranış ve duygular üzerine bilimsel ve popüler eserler." },
        new() { Id = 14, Name = "Sanat", Icon = "🎨", IconBackground = Color.FromArgb("#FFF8E1"), Description = "Resim, müzik, tiyatro ve yaratıcı disiplinlere dair kitaplar." },
        new() { Id = 15, Name = "Felsefe", Icon = "💭", IconBackground = Color.FromArgb("#EDE7F6"), Description = "Varoluş, bilgi ve ahlak üzerine düşünsel sorgulamalar." },
        new() { Id = 16, Name = "Bilim", Icon = "🔬", IconBackground = Color.FromArgb("#E0F7FA"), Description = "Doğa, evren ve bilimsel keşifleri anlaşılır dille aktaran eserler." },
        new() { Id = 17, Name = "Ekonomi", Icon = "💰", IconBackground = Color.FromArgb("#FFF3E0"), Description = "Piyasalar, finans ve ekonomik düşünceyi ele alan kitaplar." },
        new() { Id = 18, Name = "Hukuk", Icon = "⚖️", IconBackground = Color.FromArgb("#ECEFF1"), Description = "Hukuk sistemi, haklar ve adalet konularını inceleyen kaynaklar." },
        new() { Id = 19, Name = "Din", Icon = "🕌", IconBackground = Color.FromArgb("#F1F8E9"), Description = "İnanç, maneviyat ve dini metinler üzerine okumalar." },
        new() { Id = 20, Name = "Gezi", Icon = "✈️", IconBackground = Color.FromArgb("#E1F5FE"), Description = "Şehirler, ülkeler ve kültürler üzerine gezi ve seyahat yazıları." },
        new() { Id = 21, Name = "Dünya Klasikleri", Icon = "🌍", IconBackground = Color.FromArgb("#FFF9C4"), Description = "Dünya edebiyatının zamansız ve evrensel başyapıtları." },
        new() { Id = 22, Name = "Türk Klasikleri", Icon = "🇹🇷", IconBackground = Color.FromArgb("#FFEBEE"), Description = "Türk edebiyatının köklü ve unutulmaz eserleri." }
    ];

    public static BookCategoryItem? GetById(int id)
        => All.FirstOrDefault(c => c.Id == id);

    public static int GetDemoCount(int categoryId)
        => DemoBookCounts.GetValueOrDefault(categoryId, 0);

    public static IReadOnlyList<string> DisplayNames { get; } =
        All.Select(c => c.Name).ToList();

  // Backend bağlanana kadar örnek kitap sayıları (fotoğraftaki gibi)
    private static readonly Dictionary<int, int> DemoBookCounts = new()
    {
        [1] = 4, [2] = 6, [3] = 5, [4] = 7, [5] = 3, [6] = 8,
        [7] = 4, [8] = 3, [9] = 2, [10] = 5, [11] = 3, [12] = 9,
        [13] = 4, [14] = 3, [15] = 5, [16] = 6, [17] = 4, [18] = 2,
        [19] = 3, [20] = 4, [21] = 7, [22] = 5
    };

    public static List<BookCategoryItem> CreateListWithCounts(
        IEnumerable<Book> books,
        bool useDemoCountsWhenEmpty = false)
    {
        var bookList = books.ToList();
        var hasAnyBooks = bookList.Count > 0;

        return All.Select(cat =>
        {
            var count = bookList.Count(b => MatchesBook(cat.Id, b.Category));
            if (count == 0 && useDemoCountsWhenEmpty && !hasAnyBooks)
                count = DemoBookCounts.GetValueOrDefault(cat.Id, 0);

            return new BookCategoryItem
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                Icon = cat.Icon,
                IconBackground = cat.IconBackground,
                BookCount = count
            };
        }).ToList();
    }

    public static List<Book> FilterBooks(IEnumerable<Book> books, int categoryId)
        => books.Where(b => MatchesBook(categoryId, b.Category)).ToList();

    public static bool MatchesBook(int categoryId, string? bookCategory)
    {
        if (string.IsNullOrWhiteSpace(bookCategory)) return false;

        var normalizedBook = Normalize(bookCategory);
        foreach (var candidate in GetMatchTokens(categoryId))
        {
            if (normalizedBook == Normalize(candidate) ||
                normalizedBook.Contains(Normalize(candidate), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> GetMatchTokens(int categoryId) => categoryId switch
    {
        1 => ["polisiye"],
        2 => ["bilimkurgu", "bilim kurgu", "bilim-kurgu"],
        3 => ["fantastik"],
        4 => ["tarih"],
        5 => ["biyografi"],
        6 => ["çocuk", "cocuk"],
        7 => ["macera"],
        8 => ["dram"],
        9 => ["korku"],
        10 => ["hikaye"],
        11 => ["şiir", "siir"],
        12 => ["kişisel gelişim", "kisisel gelisim", "kişisel_gelişim", "kisisel_gelisim"],
        13 => ["psikoloji"],
        14 => ["sanat"],
        15 => ["felsefe"],
        16 => ["bilim"],
        17 => ["ekonomi"],
        18 => ["hukuk"],
        19 => ["din"],
        20 => ["gezi"],
        21 => ["dünya klasikleri", "dunya klasikleri", "dünya_klasikleri"],
        22 => ["türk klasikleri", "turk klasikleri", "türk_klasikleri"],
        _ => []
    };

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ");
}
