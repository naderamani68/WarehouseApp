using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>کلیدهای تنظیمات — یک‌جا تعریف شده تا تایپی وارد نشود</summary>
public static class SettingKeys
{
    // ===== مالیات =====
    public const string VatPercent = "Tax.VatPercent";
    public const string VatEnabled = "Tax.VatEnabled";
    public const string VatOnPurchase = "Tax.VatOnPurchase";

    // ===== اطلاعات شرکت (برای سرصفحهٔ فاکتور) =====
    public const string CompanyName = "Company.Name";
    public const string CompanyPhone = "Company.Phone";
    public const string CompanyAddress = "Company.Address";
    public const string CompanyEconomicCode = "Company.EconomicCode";
    public const string CompanyNationalId = "Company.NationalId";

    // ===== فروش =====
    public const string InvoicePrefix = "Sales.InvoicePrefix";
    public const string DefaultPaymentDays = "Sales.DefaultPaymentDays";
}

/// <summary>
/// خواندن و نوشتن تنظیمات.
/// مقدارها متنی ذخیره می‌شوند و هنگام خواندن به عدد یا بله/خیر تبدیل می‌گردند.
/// </summary>
public static class SettingsService
{
    public static async Task<List<AppSetting>> GetAllAsync(ApplicationDbContext db)
    {
        return await db.AppSettings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.SortOrder)
            .ToListAsync();
    }

    public static async Task<string?> GetAsync(ApplicationDbContext db, string key)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    /// <summary>خواندن مقدار عددی — اگر تنظیم نبود یا نامعتبر بود، مقدار پیش‌فرض برگردانده می‌شود</summary>
    public static async Task<decimal> GetDecimalAsync(ApplicationDbContext db, string key, decimal fallback = 0m)
    {
        var text = await GetAsync(db, key);
        return ParseDecimal(text) ?? fallback;
    }

    /// <summary>خواندن مقدار بله/خیر</summary>
    public static async Task<bool> GetBoolAsync(ApplicationDbContext db, string key, bool fallback = false)
    {
        var text = await GetAsync(db, key);
        return ParseBool(text) ?? fallback;
    }

    /// <summary>نرخ مالیات بر ارزش افزوده — پیش‌فرض ۱۰ درصد</summary>
    public static Task<decimal> VatPercentAsync(ApplicationDbContext db)
        => GetDecimalAsync(db, SettingKeys.VatPercent, 10m);

    public static Task<bool> VatEnabledAsync(ApplicationDbContext db)
        => GetBoolAsync(db, SettingKeys.VatEnabled, false);

    /// <summary>ذخیرهٔ یک تنظیم (اگر نبود، ساخته می‌شود) همراه با ثبت رویداد</summary>
    public static async Task<(bool Ok, string Message)> SetAsync(
        ApplicationDbContext db, string key, string? value, string? username)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting is null)
        {
            return (false, $"تنظیم «{key}» پیدا نشد");
        }

        // اعتبارسنجی بر اساس نوع مقدار
        if (setting.ValueType is "number" or "percent")
        {
            if (ParseDecimal(value) is null)
            {
                return (false, $"مقدار «{setting.Title}» باید عدد باشد");
            }
        }

        var old = setting.Value;
        setting.Value = value?.Trim();
        setting.UpdatedAt = DateTime.Now;
        setting.Username = username;

        db.AuditLogs.Add(new AuditLog
        {
            Action = "ویرایش",
            Entity = "تنظیمات",
            Details = $"تنظیم «{setting.Title}» از «{Show(old)}» به «{Show(setting.Value)}» تغییر کرد",
            Username = username
        });

        await db.SaveChangesAsync();

        return (true, $"تنظیم «{setting.Title}» ذخیره شد");
    }

    /// <summary>ذخیرهٔ گروهی — برای دکمهٔ «ذخیرهٔ همه»</summary>
    public static async Task<int> SetManyAsync(
        ApplicationDbContext db, IDictionary<string, string?> values, string? username)
    {
        var keys = values.Keys.ToList();
        var settings = await db.AppSettings.Where(s => keys.Contains(s.Key)).ToListAsync();

        var changed = new List<string>();

        foreach (var setting in settings)
        {
            var newValue = values[setting.Key]?.Trim();

            if (setting.Value == newValue)
            {
                continue;
            }

            if (setting.ValueType is "number" or "percent" && ParseDecimal(newValue) is null)
            {
                continue;
            }

            changed.Add($"{setting.Title}: «{Show(setting.Value)}» ← «{Show(newValue)}»");
            setting.Value = newValue;
            setting.UpdatedAt = DateTime.Now;
            setting.Username = username;
        }

        if (changed.Count == 0)
        {
            return 0;
        }

        db.AuditLogs.Add(new AuditLog
        {
            Action = "ویرایش",
            Entity = "تنظیمات",
            Details = "تغییر تنظیمات — " + string.Join(" ؛ ", changed),
            Username = username
        });

        await db.SaveChangesAsync();

        return changed.Count;
    }

    /// <summary>تجزیهٔ عدد با پشتیبانی از ارقام و جداکنندهٔ فارسی</summary>
    public static decimal? ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var clean = text.Trim()
            .Replace("٫", ".")
            .Replace("٬", "")
            .Replace(",", "")
            .Replace("٪", "")
            .Replace("%", "");

        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static bool? ParseBool(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var clean = text.Trim().ToLowerInvariant();

        return clean switch
        {
            "true" or "1" or "yes" or "on" or "بله" or "فعال" => true,
            "false" or "0" or "no" or "off" or "خیر" or "غیرفعال" => false,
            _ => null
        };
    }

    private static string Show(string? value)
        => string.IsNullOrWhiteSpace(value) ? "خالی" : value;
}
