namespace WarehouseApp.Services;

/// <summary>
/// فایل‌های نمونهٔ اکسل برای وارد کردن داده.
/// عنوان ستون‌ها دقیقاً همان چیزی است که صفحهٔ کالاها می‌خواند،
/// پس فایل ساخته‌شده بدون تغییر هم قابل استفاده است.
/// </summary>
public static class ExcelTemplates
{
    /// <summary>
    /// نمونهٔ ورود گروهی کالاها.
    /// ترتیب ستون‌ها مهم است: کد، نام، واحد، قیمت فروش، بهای خرید، موجودی، حد سفارش، دسته‌بندی.
    /// </summary>
    public static byte[] Products()
    {
        var columns = new Dictionary<string, Func<object?, object?>>
        {
            ["کد کالا"] = _ => null,
            ["نام کالا"] = _ => null,
            ["واحد"] = _ => null,
            ["قیمت فروش"] = _ => null,
            ["بهای خرید"] = _ => null,
            ["موجودی"] = _ => null,
            ["حد سفارش"] = _ => null,
            ["دسته‌بندی"] = _ => null
        };

        return new ExcelService().Export(Array.Empty<object?>(), "کالاها", columns);
    }

    /// <summary>
    /// نمونهٔ ورود طرف حساب‌ها.
    /// ترتیب: کد، نام، نوع، شخص رابط، تلفن، موبایل، ایمیل، کد ملی، کد اقتصادی، شهر، نشانی، کد پستی، یادداشت.
    /// </summary>
    public static byte[] Parties()
    {
        var columns = new Dictionary<string, Func<object?, object?>>
        {
            ["کد"] = _ => null,
            ["نام"] = _ => null,
            ["نوع"] = _ => null,
            ["شخص رابط"] = _ => null,
            ["تلفن"] = _ => null,
            ["موبایل"] = _ => null,
            ["ایمیل"] = _ => null,
            ["کد ملی"] = _ => null,
            ["کد اقتصادی"] = _ => null,
            ["شهر"] = _ => null,
            ["نشانی"] = _ => null,
            ["کد پستی"] = _ => null,
            ["یادداشت"] = _ => null
        };

        return new ExcelService().Export(Array.Empty<object?>(), "طرف حساب‌ها", columns);
    }

    /// <summary>نمونهٔ ورود دسته‌بندی‌ها — ترتیب: نام دسته، دستهٔ مادر، توضیح</summary>
    public static byte[] Categories()
    {
        var columns = new Dictionary<string, Func<object?, object?>>
        {
            ["نام دسته"] = _ => null,
            ["دستهٔ مادر"] = _ => null,
            ["توضیح"] = _ => null
        };

        return new ExcelService().Export(Array.Empty<object?>(), "دسته‌بندی‌ها", columns);
    }

    /// <summary>
    /// نمونهٔ ورود قیمت‌ها.
    /// شناسایی با عنوان ستون انجام می‌شود، پس ترتیب مهم نیست.
    /// </summary>
    public static byte[] Prices()
    {
        var columns = new Dictionary<string, Func<object?, object?>>
        {
            ["کد کالا"] = _ => null,
            ["نام کالا"] = _ => null,
            ["قیمت فروش"] = _ => null,
            ["بهای خرید"] = _ => null
        };

        return new ExcelService().Export(Array.Empty<object?>(), "قیمت‌ها", columns);
    }
}