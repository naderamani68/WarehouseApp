using System.Globalization;

namespace WarehouseApp.Services;

/// <summary>
/// تبدیل تاریخ میلادی به شمسی برای نمایش در جدول‌ها و خروجی‌ها.
/// همه‌جای CRM از همین کمک‌کار استفاده می‌کند تا قالب تاریخ یکسان بماند.
/// </summary>
public static class PersianDate
{
    private static readonly PersianCalendar Pc = new();

    /// <summary>تاریخ شمسی — مثل 1405/07/16</summary>
    public static string ToDate(DateTime dt)
        => $"{Pc.GetYear(dt):0000}/{Pc.GetMonth(dt):00}/{Pc.GetDayOfMonth(dt):00}";

    /// <summary>تاریخ و ساعت شمسی — مثل 1405/07/16 - 17:24</summary>
    public static string ToDateTime(DateTime dt)
        => $"{ToDate(dt)} - {dt:HH:mm}";

    /// <summary>تاریخ شمسی یا خط تیره وقتی مقدار خالی است</summary>
    public static string OrDash(DateTime? dt)
        => dt.HasValue ? ToDate(dt.Value) : "—";

    /// <summary>تاریخ و ساعت شمسی یا خط تیره</summary>
    public static string DateTimeOrDash(DateTime? dt)
        => dt.HasValue ? ToDateTime(dt.Value) : "—";

    /// <summary>تبدیل رقم‌های لاتین به فارسی برای نمایش</summary>
    public static string ToPersianDigits(int number) => ToPersianDigits(number.ToString(CultureInfo.InvariantCulture));

    /// <summary>تبدیل رقم‌های لاتین به فارسی در یک متن</summary>
    public static string ToPersianDigits(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(text.Length);

        foreach (var ch in text)
        {
            builder.Append(ch is >= '0' and <= '9' ? (char)('۰' + (ch - '0')) : ch);
        }

        return builder.ToString();
    }

    /// <summary>خواندن تاریخ شمسی از متن ورودی — مثل 1405/07/16 یا ۱۴۰۵/۰۷/۱۶</summary>
    public static DateTime? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var digits = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            if (ch is >= '0' and <= '9')
            {
                digits.Append(ch);
            }
            else if (ch is >= '۰' and <= '۹')
            {
                digits.Append((char)('0' + (ch - '۰')));
            }
            else if (ch is '/' or '-' or '.' or ' ')
            {
                digits.Append(' ');
            }
        }

        var parts = digits.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var day))
        {
            return null;
        }

        if (year < 1300 || year > 1500 || month < 1 || month > 12 || day < 1 || day > 31)
        {
            return null;
        }

        try
        {
            return Pc.ToDateTime(year, month, day, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>سال شمسی جاری — برای شماره‌گذاری سفارش‌ها</summary>
    public static int CurrentYear => Pc.GetYear(DateTime.Now);
}
