namespace LibrarySystem.UI.Helpers;

public static class BookCoverHelper
{
    public static async Task SetCoverAsync(
        Image image,
        Label? placeholder,
        int bookId,
        CancellationToken cancellationToken = default)
    {
        image.IsVisible = false;
        image.Source = null;
        if (placeholder is not null)
        {
            placeholder.IsVisible = true;
        }

        if (bookId <= 0)
        {
            return;
        }

        try
        {
            using var client = ApiClientHelper.CreateClient();
            using var response = await client.GetAsync($"/books/{bookId}/cover", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
            {
                return;
            }

            image.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            image.IsVisible = true;
            if (placeholder is not null)
            {
                placeholder.IsVisible = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Yeni yükleme isteği geldi.
        }
        catch
        {
            // Kapak yoksa placeholder kalır.
        }
    }
}
