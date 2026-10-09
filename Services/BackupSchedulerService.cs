namespace WarehouseApp.Services;

/// <summary>
/// زمان‌بند پشتیبان‌گیری خودکار.
///
/// هر پانزده دقیقه یک بار بررسی می‌کند: اگر پشتیبان‌گیری خودکار روشن باشد،
/// به ساعت مقرر رسیده باشیم و امروز پشتیبانی گرفته نشده باشد، پشتیبان می‌گیرد.
///
/// اگر برنامه در ساعت مقرر خاموش بوده باشد، اولین بررسی پس از روشن‌شدن برنامه
/// این کار را انجام می‌دهد؛ پس پشتیبان از دست نمی‌رود.
/// </summary>
public class BackupSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<BackupSchedulerService> logger;

    public BackupSchedulerService(IServiceScopeFactory scopeFactory, ILogger<BackupSchedulerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // کمی صبر تا برنامه کامل بالا بیاید
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var backup = scope.ServiceProvider.GetRequiredService<BackupService>();
                    await backup.RunAutoAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "خطا در پشتیبان‌گیری خودکار");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // خاموش‌شدن برنامه — طبیعی است
        }
    }
}
