using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
namespace WarehouseApp.Services;

/// <summary>
/// فایل‌های نمونهٔ اکسل برای وارد کردن داده.
/// عنوان ستون‌ها دقیقاً همان چیزی است که صفحهٔ کالاها می‌خواند،
/// پس فایل ساخته‌شده بدون تغییر هم قابل استفاده است.
/// </summary>
public static class ExcelTemplates
{
    /// <summary>برگهٔ دوم: فهرست‌های پویا از روی دادهٔ خود سیستم</summary>
    private static byte[] WithLists(byte[] file, IEnumerable<(string Title, IEnumerable<string?> Values)> lists)
    {
        using var input = new MemoryStream(file);
        using var workbook = new XLWorkbook(input);

        var sheet = workbook.Worksheets.Add("فهرست‌ها");
        var column = 1;

        foreach (var (title, values) in lists)
        {
            var head = sheet.Cell(1, column);
            head.Value = title;
            head.Style.Font.Bold = true;
            head.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var cell = sheet.Cell(row, column);
                cell.Value = value;
                cell.Style.NumberFormat.Format = "@";
                row++;
            }

            sheet.Column(column).Width = 34;
            column++;
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public static async Task<byte[]> ProductsAsync(WarehouseApp.Data.ApplicationDbContext db)
    {
        var categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        var units = new[] { "عدد", "کارتن", "کیلوگرم", "لیتر", "بسته", "جعبه", "متر", "خدمت" };

        return WithLists(Products(), new (string, IEnumerable<string?>)[]
        {
            ("دسته‌بندی — فقط از این فهرست انتخاب کن", categories),
            ("واحد — خالی بگذاری «عدد» می‌شود", units)
        });
    }

    public static async Task<byte[]> PricesAsync(WarehouseApp.Data.ApplicationDbContext db)
    {
        var products = await db.Products
            .Where(p => p.Code != null)
            .OrderBy(p => p.Name)
            .Select(p => p.Code + " — " + p.Name)
            .ToListAsync();

        return WithLists(Prices(), new (string, IEnumerable<string?>)[]
        {
            ("کالاهای موجود — کد و نام", products)
        });
    }

    public static async Task<byte[]> PartiesAsync(WarehouseApp.Data.ApplicationDbContext db)
    {
        var parties = await db.Parties
            .OrderBy(p => p.Name)
            .Select(p => p.Code + " — " + p.Name)
            .ToListAsync();

        return WithLists(Parties(), new (string, IEnumerable<string?>)[]
        {
            ("نوع — فقط یکی از این سه", new[] { "مشتری", "تأمین‌کننده", "هر دو" }),
            ("طرف حساب‌های موجود", parties)
        });
    }

    public static async Task<byte[]> CategoriesAsync(WarehouseApp.Data.ApplicationDbContext db)
    {
        var names = await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        return WithLists(Categories(), new (string, IEnumerable<string?>)[]
        {
            ("دسته‌های موجود — برای ستون دستهٔ مادر", names)
        });
    }
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