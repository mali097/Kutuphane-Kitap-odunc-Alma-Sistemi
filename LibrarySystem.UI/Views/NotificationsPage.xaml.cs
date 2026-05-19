using Microsoft.Maui.Storage;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class NotificationsPage : ContentPage
{
    private const string ReadIdsKey = "delivery_notif_read_ids";
    private readonly IBorrowService _borrowService;
    private List<DeliveryNotificationItem> _all = new();

    private enum NotifTab { All, Unread, Read }
    private NotifTab _currentTab = NotifTab.All;

    public NotificationsPage()
    {
        InitializeComponent();
        _borrowService = new BorrowService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var user = SessionHelper.CurrentUser;
        if (user == null)
        {
            _all = new();
            ApplyFilter();
            return;
        }

        var borrows = await _borrowService.GetUserBorrowsAsync(user.Id);
        var readIds = LoadReadIds();
        var list = new List<DeliveryNotificationItem>();

        foreach (var b in borrows.OrderByDescending(x => x.BorrowDate))
        {
            var receivedId = $"recv-{b.Id}";
            list.Add(new DeliveryNotificationItem
            {
                Id = receivedId,
                Title = "Teslim aldınız",
                Detail = $"“{b.BookTitle}” ödünç kaydınız oluşturuldu. Son teslim: {b.DueDate:dd.MM.yyyy}.",
                OccurredAt = b.BorrowDate,
                RelativeTimeText = ToRelativeTr(b.BorrowDate),
                IconGlyph = "📗",
                IconBackground = Color.FromArgb("#5E4BB6"),
                StatusDotColor = readIds.Contains(receivedId) ? Color.FromArgb("#C5C2CC") : Color.FromArgb("#3B7CFF"),
                IsRead = readIds.Contains(receivedId)
            });

            if (b.IsReturned && b.ReturnDate.HasValue)
            {
                var returnedId = $"ret-{b.Id}";
                var rd = b.ReturnDate.Value;
                list.Add(new DeliveryNotificationItem
                {
                    Id = returnedId,
                    Title = "Kitap teslim edildiniz",
                    Detail = $"“{b.BookTitle}” iade işleminiz tamamlandı. İade tarihi: {rd:dd.MM.yyyy}.",
                    OccurredAt = rd,
                    RelativeTimeText = ToRelativeTr(rd),
                    IconGlyph = "✅",
                    IconBackground = Color.FromArgb("#2EA77E"),
                    StatusDotColor = readIds.Contains(returnedId) ? Color.FromArgb("#C5C2CC") : Color.FromArgb("#3B7CFF"),
                    IsRead = readIds.Contains(returnedId)
                });
            }
        }

        _all = list.OrderByDescending(n => n.OccurredAt).ToList();
        ApplyFilter();
    }

    private static HashSet<string> LoadReadIds()
    {
        var raw = Preferences.Get(ReadIdsKey, string.Empty);
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
    }

    private static void SaveReadIds(HashSet<string> ids)
        => Preferences.Set(ReadIdsKey, string.Join(',', ids));

    private static string ToRelativeTr(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalSeconds < 45) return "Az önce";
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)} dk önce";
        if (span.TotalHours < 24) return $"{Math.Max(1, (int)span.TotalHours)} saat önce";
        if (span.TotalDays < 7) return $"{Math.Max(1, (int)span.TotalDays)} gün önce";
        return dt.ToString("dd.MM.yyyy");
    }

    private void ApplyFilter()
    {
        IEnumerable<DeliveryNotificationItem> q = _all;
        q = _currentTab switch
        {
            NotifTab.Unread => q.Where(n => !n.IsRead),
            NotifTab.Read => q.Where(n => n.IsRead),
            _ => q
        };
        NotificationsList.ItemsSource = q.ToList();
    }

    private void SetTabVisual(NotifTab tab)
    {
        _currentTab = tab;
        var active = Colors.White;
        var inactive = Color.FromArgb("#E8E0FF");

        TabAll.FontAttributes = tab == NotifTab.All ? FontAttributes.Bold : FontAttributes.None;
        TabAll.TextColor = tab == NotifTab.All ? active : inactive;
        TabUnread.FontAttributes = tab == NotifTab.Unread ? FontAttributes.Bold : FontAttributes.None;
        TabUnread.TextColor = tab == NotifTab.Unread ? active : inactive;
        TabRead.FontAttributes = tab == NotifTab.Read ? FontAttributes.Bold : FontAttributes.None;
        TabRead.TextColor = tab == NotifTab.Read ? active : inactive;
    }

    private void TabAll_Tapped(object? sender, EventArgs e)
    {
        SetTabVisual(NotifTab.All);
        ApplyFilter();
    }

    private void TabUnread_Tapped(object? sender, EventArgs e)
    {
        SetTabVisual(NotifTab.Unread);
        ApplyFilter();
    }

    private void TabRead_Tapped(object? sender, EventArgs e)
    {
        SetTabVisual(NotifTab.Read);
        ApplyFilter();
    }

    private void OnNotificationTapped(object? sender, EventArgs e)
    {
        if (sender is not VisualElement { BindingContext: DeliveryNotificationItem item })
            return;

        var ids = LoadReadIds();
        if (!ids.Contains(item.Id))
        {
            ids.Add(item.Id);
            SaveReadIds(ids);
        }

        item.IsRead = true;
        item.StatusDotColor = Color.FromArgb("#C5C2CC");
        ApplyFilter();
    }

    private async void Back_Clicked(object? sender, EventArgs e)
        => await Navigation.PopAsync();

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabCategories_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToCategoriesAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToSettingsAsync();
}
