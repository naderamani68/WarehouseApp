using System.Text;

namespace WarehouseApp.Services;

/// <summary>
/// قانون یکسان برای شمارهٔ همراه ایران: ۱۱ رقم و شروع با ۰۹.
///
/// توجه: تلفن ثابت از این قانون پیروی نمی‌کند؛ این کلاس فقط برای
/// فیلدهای «موبایل» و «تلفن همراه» استفاده می‌شود.
/// </summary>
public static class Mobile
{
    public const int Length = 11;
    public const string Prefix = "09";

    /// <summary>فقط رقم‌ها را نگه می‌دارد؛ رقم فارسی و عربی را به لاتین تبدیل می‌کند</summary>
    public static string DigitsOnly(string? value, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var ch in value)
        {
            char? digit = ch switch
            {
                >= '0' and <= '9' => ch,
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                _ => null
            };

            if (digit is null)
            {
                continue;
            }

            builder.Append(digit.Value);

            if (maxLength > 0 && builder.Length >= maxLength)
            {
                break;
            }
        }

        return builder.ToString();
    }

    /// <summary>یکسان‌سازی کامل: حذف کد کشور و افزودن صفر ابتدایی — کوتاه‌کردن انجام نمی‌شود</summary>
    public static string Normalize(string? value)
    {
        var digits = DigitsOnly(value);

        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.StartsWith("0098"))
        {
            digits = digits[4..];
        }
        else if (digits.StartsWith("98") && digits.Length >= 12)
        {
            digits = digits[2..];
        }

        if (digits.Length == 10 && digits.StartsWith("9"))
        {
            digits = "0" + digits;
        }

        // کوتاه‌کردن انجام نمی‌شود؛ شمارهٔ بلندتر از ۱۱ رقم نامعتبر است
        return digits;
    }

    /// <summary>شمارهٔ همراه درست است؟</summary>
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);

        return digits.Length == Length && digits.StartsWith(Prefix);
    }

    /// <summary>نمایش خوانا: ۰۹۱۲ ۳۴۵ ۶۷۸۹</summary>
    public static string Format(string? value)
    {
        var digits = Normalize(value);

        return digits.Length == Length
            ? $"{digits[..4]} {digits[4..7]} {digits[7..]}"
            : digits;
    }
}
