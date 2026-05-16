namespace LibrarySystem.Api.Entities;

public static class GenreTypeListConverter
{
    public static string ToStorage(IReadOnlyCollection<GenreType> genres)
    {
        if (genres.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(',', genres.Select(static genre => (int)genre));
    }

    public static List<GenreType> FromStorage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var genres = new List<GenreType>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var numeric) && Enum.IsDefined(typeof(GenreType), numeric))
            {
                genres.Add((GenreType)numeric);
                continue;
            }

            if (Enum.TryParse<GenreType>(part, ignoreCase: true, out var parsed))
            {
                genres.Add(parsed);
            }
        }

        return genres.Distinct().ToList();
    }

    public static IReadOnlyList<string> ToGenreNames(IEnumerable<GenreType> genres)
    {
        return genres.Select(static genre => genre.ToString()).ToList();
    }

    public static (Dictionary<string, string[]> Errors, List<GenreType> ParsedGenres) ValidateAndParseNames(
        IReadOnlyList<string>? genreNames,
        bool required)
    {
        var errors = new Dictionary<string, string[]>();

        if (required && (genreNames is null || genreNames.Count == 0))
        {
            errors["genres"] = ["At least one genre is required."];
            return (errors, []);
        }

        if (genreNames is null)
        {
            return (errors, []);
        }

        var parsed = new List<GenreType>();
        var unknown = new List<string>();

        foreach (var name in genreNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                unknown.Add("(empty)");
                continue;
            }

            if (Enum.TryParse<GenreType>(name.Trim(), ignoreCase: true, out var genre))
            {
                parsed.Add(genre);
                continue;
            }

            unknown.Add(name.Trim());
        }

        if (unknown.Count > 0)
        {
            var allowed = string.Join(", ", Enum.GetNames<GenreType>());
            errors["genres"] =
            [
                $"Unknown genre(s): {string.Join(", ", unknown)}. Use genre names such as: {allowed}."
            ];
        }

        if (parsed.Distinct().Count() != parsed.Count)
        {
            errors["genres"] = ["Duplicate genres are not allowed."];
        }

        return (errors, parsed.Distinct().ToList());
    }
}
