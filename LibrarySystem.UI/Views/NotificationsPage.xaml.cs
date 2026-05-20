using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class NotificationsPage : ContentPage
{
    private readonly INotificationService _notificationService;
    private readonly IBorrowService _borrowService;
    private List<DeliveryNotificationItem> _all = new();

    private enum NotifTab { All, Unread, Read }
    private NotifTab _currentTab = NotifTab.All;

    public NotificationsPage()
    {
        InitializeComponent();
        _notificationService = new NotificationService();
        _borrowService = new BorrowService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeHelper.ApplyBottomTab(
            TabNotificationsBtn,
            TabHomeBtn, TabCategoriesBtn, TabFavoritesBtn, TabSettingsBtn);
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

        _all = await _notificationService.GetUserNotificationsAsync(user.Id);

        if (_all.Count == 0)
        {
            _all = await BuildFallbackFromBorrowsAsync(user.Id);
        }

        ApplyFilter();
    }

    private async Task<List<DeliveryNotificationItem>> BuildFallbackFromBorrowsAsync(int userId)
    {
        var borrows = await _borrowService.GetMyBorrowsAsync();
        var list = new List<DeliveryNotificationItem>();

        foreach (var b in borrows.OrderByDescending(x => x.BorrowDate))
        {
            list.Add(new DeliveryNotificationItem
            {
                Id = $"recv-{b.Id}",
                Title = "Kitap teslim alındı",
                Detail = $"{b.BorrowDate:dd.MM.yyyy} tarihinde \"{b.BookTitle}\" kitabını teslim aldınız. {b.DueDate:dd.MM.yyyy} tarihine kadar kitabı teslim etmeniz gerekiyor.",
                OccurredAt = b.BorrowDate,
                RelativeTimeText = ToRelativeTr(b.BorrowDate),
                IconGlyph = "📗",
                IconBackground = Color.FromArgb("#5E4BB6"),
                StatusDotColor = Color.FromArgb("#3B7CFF"),
                IsRead = false
            });

            if (b.IsReturned && b.ReturnDate.HasValue)
            {
                var rd = b.ReturnDate.Value;
                var daysLate = (rd.Date - b.DueDate.Date).Days;
                var onTime = daysLate <= 0;

                list.Add(new DeliveryNotificationItem
                {
                    Id = $"ret-{b.Id}",
                    Title = onTime ? "Zamanında iade" : "Gecikmiş iade",
                    Detail = onTime
                        ? $"{rd:dd.MM.yyyy} tarihinde \"{b.BookTitle}\" kitabını zamanında teslim ettiniz."
                        : $"{rd:dd.MM.yyyy} tarihinde \"{b.BookTitle}\" kitabını {daysLate} gün geç teslim ettiniz.",
                    OccurredAt = rd,
                    RelativeTimeText = ToRelativeTr(rd),
                    IconGlyph = onTime ? "✅" : "⚠️",
                    IconBackground = Color.FromArgb(onTime ? "#2EA77E" : "#C62828"),
                    StatusDotColor = Color.FromArgb(onTime ? "#2EA77E" : "#C62828"),
                    IsRead = false
                });
            }
        }

        return list.OrderByDescending(n => n.OccurredAt).ToList();
    }

    private static string ToRelativeTr(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var span = DateTime.Now - local;
        if (span.TotalSeconds < 45) return "Az önce";
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)} dk önce";
        if (span.TotalHours < 24) return $"{Math.Max(1, (int)span.TotalHours)} saat önce";
        if (span.TotalDays < 7) return $"{Math.Max(1, (int)span.TotalDays)} gün önce";
        return local.ToString("dd.MM.yyyy");
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

    private async void OnNotificationTapped(object? sender, EventArgs e)
    {
        if (sender is not VisualElement { BindingContext: DeliveryNotificationItem item })
            return;

        if (!item.IsRead && int.TryParse(item.Id, out var notificationId))
        {
            await _notificationService.MarkAsReadAsync(notificationId);
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
