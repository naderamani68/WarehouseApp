using System.Globalization;

namespace WarehouseApp.Services;

/// <summary>
/// سبد خرید فروشگاه — در یک کوکی سبک نگه داشته می‌شود.
/// چون صفحه‌های فروشگاه استاتیک هستند، سبد نباید به اتصال زندهٔ سروری وابسته باشد.
/// قالب مقدار کوکی: «شناسه:تعداد» و اقلام با کاما جدا می‌شوند؛ مثال: 12:2,15:1
/// </summary>
public static class ShopCart
{
    public const string CookieName = "WarehouseApp.Cart";

    private const int MaxItems = 30;
    private const int MaxQuantity = 999;

    /// <summary>خواندن سبد از کوکی</summary>
    public static Dictionary<int, int> Read(HttpContext context)
    {
        var result = new Dictionary<int, int>();

        var raw = context.Request.Cookies[CookieName];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split(':');

            if (pieces.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(pieces[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
            {
                continue;
            }

            if (!int.TryParse(pieces[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
            {
                continue;
            }

            result[id] = Math.Clamp(quantity, 1, MaxQuantity);
        }

        return result;
    }

    /// <summary>نوشتن سبد در کوکی</summary>
    public static void Write(HttpContext context, Dictionary<int, int> items)
    {
        var value = string.Join(',', items
            .Where(x => x.Key > 0 && x.Value > 0)
            .OrderBy(x => x.Key)
            .Take(MaxItems)
            .Select(x => $"{x.Key}:{Math.Clamp(x.Value, 1, MaxQuantity)}"));

        context.Response.Cookies.Append(CookieName, value, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Expires = DateTimeOffset.Now.AddDays(14),
            Path = "/"
        });
    }

    /// <summary>خالی کردن سبد</summary>
    public static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }

    /// <summary>جمع تعداد اقلام سبد</summary>
    public static int CountItems(Dictionary<int, int> items) => items.Values.Sum();
}
