namespace LibrarySystem.Api.Services;

public static class IsbnGenerator
{
    /// <summary>Generates a unique ISBN-13 for the library (prefix 978606 + 7-digit book id + check digit).</summary>
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
        return withoutCheckDigit + ComputeIsbn13CheckDigit(withoutCheckDigit);
    }

    private static char ComputeIsbn13CheckDigit(string isbn12Digits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = isbn12Digits[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }
}
