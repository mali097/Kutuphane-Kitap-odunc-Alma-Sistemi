namespace LibrarySystem.Api.Services;

public sealed class BorrowReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public BorrowReminderBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<IBorrowNotificationService>();
                await notificationService.ProcessDueDateRemindersAsync(stoppingToken);
            }
            catch
            {
                // Background reminder failures should not stop the host.
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
