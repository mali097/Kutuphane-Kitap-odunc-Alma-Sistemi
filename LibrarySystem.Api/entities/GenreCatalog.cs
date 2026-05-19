using System.Globalization;

namespace LibrarySystem.Api.Entities;

public static class GenreCatalog
{
  private static readonly Dictionary<string, GenreType> Aliases = BuildAliases();

  public static IReadOnlyList<string> AllNames { get; } =
      Enum.GetValues<GenreType>()
          .Cast<GenreType>()
          .OrderBy(genre => (int)genre)
          .Select(genre => genre.ToString())
          .ToList();

  public static bool TryParse(string? input, out GenreType genre)
  {
    genre = default;
    if (string.IsNullOrWhiteSpace(input))
    {
      return false;
    }

    var normalized = NormalizeKey(input);
    if (Aliases.TryGetValue(normalized, out genre))
    {
      return true;
    }

    return Enum.TryParse(normalized, ignoreCase: true, out genre)
        && Enum.IsDefined(genre);
  }

  public static string ToStorageName(GenreType genre) => genre.ToString();

  public static string GetDisplayName(GenreType genre) => genre switch
  {
    GenreType.polisiye => "Polisiye",
    GenreType.bilimkurgu => "Bilim Kurgu",
    GenreType.fantastik => "Fantastik",
    GenreType.tarih => "Tarih",
    GenreType.biyografi => "Biyografi",
    GenreType.cocuk => "Çocuk",
    GenreType.macera => "Macera",
    GenreType.dram => "Dram",
    GenreType.korku => "Korku",
    GenreType.hikaye => "Hikaye",
    GenreType.siir => "Şiir",
    GenreType.kisisel_gelisim => "Kişisel Gelişim",
    GenreType.psikoloji => "Psikoloji",
    GenreType.sanat => "Sanat",
    GenreType.felsefe => "Felsefe",
    GenreType.bilim => "Bilim",
    GenreType.ekonomi => "Ekonomi",
    GenreType.hukuk => "Hukuk",
    GenreType.din => "Din",
    GenreType.gezi => "Gezi",
    GenreType.dunya_klasikleri => "Dünya Klasikleri",
    GenreType.turk_klasikleri => "Türk Klasikleri",
    _ => genre.ToString()
  };

  public static string? NormalizeStoredGenre(string? stored)
  {
    if (TryParse(stored, out var genre))
    {
      return ToStorageName(genre);
    }

    return null;
  }

  private static string NormalizeKey(string input)
  {
    var trimmed = input.Trim().ToLowerInvariant();
    trimmed = trimmed
        .Replace('ç', 'c')
        .Replace('ğ', 'g')
        .Replace('ı', 'i')
        .Replace('ö', 'o')
        .Replace('ş', 's')
        .Replace('ü', 'u');
    trimmed = trimmed.Replace(' ', '_').Replace('-', '_');
    return trimmed;
  }

  private static Dictionary<string, GenreType> BuildAliases()
  {
    var map = new Dictionary<string, GenreType>(StringComparer.OrdinalIgnoreCase);

    foreach (var genre in Enum.GetValues<GenreType>())
    {
      map[NormalizeKey(genre.ToString())] = genre;
      map[NormalizeKey(GetDisplayName(genre))] = genre;
      map[((int)genre).ToString(CultureInfo.InvariantCulture)] = genre;
    }

    map[NormalizeKey("çocuk")] = GenreType.cocuk;
    map[NormalizeKey("cocuk")] = GenreType.cocuk;
    map[NormalizeKey("şiir")] = GenreType.siir;
    map[NormalizeKey("siir")] = GenreType.siir;
    map[NormalizeKey("kişisel_gelişim")] = GenreType.kisisel_gelisim;
    map[NormalizeKey("kisisel_gelisim")] = GenreType.kisisel_gelisim;
    map[NormalizeKey("dünya_klasikleri")] = GenreType.dunya_klasikleri;
    map[NormalizeKey("dunya_klasikleri")] = GenreType.dunya_klasikleri;
    map[NormalizeKey("türk_klasikleri")] = GenreType.turk_klasikleri;
    map[NormalizeKey("turk_klasikleri")] = GenreType.turk_klasikleri;

    return map;
  }
}
