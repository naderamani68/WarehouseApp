namespace WarehouseApp.Data;

/// <summary>
/// پیامک ثبت‌شده — چه فرستاده شده باشد و چه منتظر پنل پیامک باشد.
/// تا وقتی پنل تنظیم نشده، پیام‌ها اینجا می‌مانند و بعداً قابل پیگیری‌اند.
/// </summary>
public class SmsMessage
{
    public int Id { get; set; }

    /// <summary>شمارهٔ گیرنده</summary>
    public string Phone { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>ارسال شد / ناموفق / بدون پنل پیامک</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>پاسخ پنل پیامک — برای پیگیری خطا</summary>
    public string? ProviderResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
