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