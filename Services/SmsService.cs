using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// فرستندهٔ پیامک.
///
/// تا وقتی پنل پیامک تنظیم نشده باشد، پیام‌ها فقط در جدول «پیامک‌ها» ثبت می‌شوند
/// و جایی فرستاده نمی‌شوند؛ پس هیچ سفارشی از کار نمی‌افتد.
///
/// برای فعال کردن، این کلیدها را در جدول تنظیمات بگذار:
/// Sms.Enabled و Sms.ApiUrl و Sms.ApiKey و Sms.Sender
/// در نشانی پنل، این جانگهدارها جایگزین می‌شوند:
/// {apiKey} و {sender} و {receptor} و {message}
/// </summary>
public class SmsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        IDbContextFactory<ApplicationDbContext> factory,
        IHttpClientFactory httpFactory,
        ILogger<SmsService> logger)
    {
        _factory = factory;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <summary>فرستادن پیامک — در صورت نبود پنل، فقط ثبت می‌شود</summary>
    public async Task<bool> SendAsync(string? phone, string text)
    {
        var receptor = ShopCatalogService.NormalizePhone(phone);

        if (receptor.Length < 10 || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        await using var db = await _factory.CreateDbContextAsync();

        var settings = await db.AppSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("Sms."))
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

        var enabled = settings.TryGetValue("Sms.Enabled", out var enabledText)
                      && (enabledText.Equals("true", StringComparison.OrdinalIgnoreCase) || enabledText == "1");

        var apiUrl = settings.TryGetValue("Sms.ApiUrl", out var urlValue) ? urlValue : string.Empty;
        var apiKey = settings.TryGetValue("Sms.ApiKey", out var keyValue) ? keyValue : string.Empty;
        var sender = settings.TryGetValue("Sms.Sender", out var senderValue) ? senderValue : string.Empty;

        var message = new SmsMessage
        {
            Phone = receptor,
            Text = text,
            CreatedAt = DateTime.Now
        };

        if (!enabled || string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            message.Status = "بدون پنل پیامک";
            db.SmsMessages.Add(message);
            await db.SaveChangesAsync();

            _logger.LogInformation("پیامک ثبت شد ولی فرستاده نشد (پنل تنظیم نشده): {Phone}", receptor);
            return false;
        }

        try
        {
            var requestUrl = apiUrl
                .Replace("{apiKey}", Uri.EscapeDataString(apiKey))
                .Replace("{sender}", Uri.EscapeDataString(sender))
                .Replace("{receptor}", Uri.EscapeDataString(receptor))
                .Replace("{message}", Uri.EscapeDataString(text));

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            var response = await client.GetAsync(requestUrl);
            var body = await response.Content.ReadAsStringAsync();

            message.Status = response.IsSuccessStatusCode ? "ارسال شد" : "ناموفق";
            message.ProviderResponse = body.Length > 500 ? body[..500] : body;

            _logger.LogInformation("پاسخ پنل پیامک: {Code}", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            message.Status = "ناموفق";
            message.ProviderResponse = ex.Message;

            _logger.LogWarning(ex, "ارسال پیامک ناموفق بود");
        }

        db.SmsMessages.Add(message);
        await db.SaveChangesAsync();

        return message.Status == "ارسال شد";
    }
}
