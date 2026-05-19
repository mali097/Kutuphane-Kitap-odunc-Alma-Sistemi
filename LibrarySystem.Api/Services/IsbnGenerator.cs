namespace LibrarySystem.Api.Services;

public static class IsbnGenerator
{
    public static string GenerateForBookId(int bookId)
    {
        if (bookId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookId), "Book id must be positive.");
        }

        if (bookId > 9_999_999)
        {
            throw new ArgumentOutOfRangeException(nameof(bookId), "Book id exceeds ISBN capacity.");
        }

        var withoutCheckDigit = $"978606{bookId:D7}";
        var sum = 0;
        for (var i = 0; i < withoutCheckDigit.Length; i++)
        {
            var digit = withoutCheckDigit[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return withoutCheckDigit + checkDigit;
    }
}
