using System.Globalization;

namespace WarehouseApp.Services;

/// <summary>
/// کمک‌کار نمایش و خواندن مبلغ.
///
/// مبلغ‌ها در سراسر برنامه شماره‌ای ذخیره می‌شوند ولی برای چشم انسان
/// با جداکنندهٔ سهرقمی نشان داده می‌شوند: 200000 → 200.000
/// </summary>
public static class MoneyText
{
    /// <summary>فقط رقم‌ها را نگه می‌دارد؛ رقم فارسی و عربی هم پذیرفته می‌شود</summary>
    public static string DigitsOnly(string? value) => Mobile.DigitsOnly(value);

    /// <summary>عدد را با جداکنندهٔ سهرقمی نقطه‌دار برمی‌گرداند: 200.000</summary>
    public static string Group(decimal value)
        => value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");

    /// <summary>عدد را به رقم خام (بدون جداکننده) تبدیل می‌کند — برای جاهایی که فیلد عدد است</summary>
    public static string FromDecimal(decimal value) => ((long)value).ToString(CultureInfo.InvariantCulture);

    /// <summary>متن نوشته‌شده را به عدد تبدیل می‌کند؛ هر چیز نامعتبر صفر می‌شود</summary>
    public static decimal Parse(string? value)
        => decimal.TryParse(DigitsOnly(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0m;
}
