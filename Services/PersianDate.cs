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

    /// <summary>سال شمسی جاری — برای شماره‌گذاری سفارش‌ها</summary>
    public static int CurrentYear => Pc.GetYear(DateTime.Now);
}
