using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// قالب‌های اکسل ورود داده.
/// هر قالب در «یک گذر» ساخته می‌شود: برگهٔ داده + برگهٔ «فهرست‌ها» با دادهٔ واقعی سیستم.
/// هر مقدار فهرست در خانهٔ جداگانه می‌نشیند تا کد و نام قاطی نشوند.
/// </summary>
public static class ExcelTemplates
{
    private static readonly string[] ProductHeaders =
        { "کد کالا", "نام کالا", "واحد", "قیمت فروش", "بهای خرید", "موجودی", "حد سفارش", "دسته‌بندی" };

    private static readonly string[] PriceHeaders =
        { "کد کالا", "نام کالا", "قیمت فروش", "بهای خرید" };

    private static readonly string[] PartyHeaders =
        { "کد", "نام", "نوع", "شخص رابط", "تلفن", "موبایل", "ایمیل", "کد ملی", "کد اقتصادی", "شهر", "نشانی", "کد پستی", "یادداشت" };

    private static readonly string[] CategoryHeaders =
        { "نام دسته", "دستهٔ مادر", "توضیح" };

    private static readonly string[] Units =
        { "عدد", "کارتن", "کیلوگرم", "لیتر", "بسته", "جعبه", "متر", "خدمت" };

    private static readonly string[] PartyTypes =
        { "مشتری", "تأمین‌کننده", "هر دو" };

    private static readonly (string[] Titles, IEnumerable<string[]> Rows)[] NoLists =
        Array.Empty<(string[], IEnumerable<string[]>)>();

    // ================= قالب‌های پایه (بدون فهرست) =================
    public static byte[] Products() => Build("کالاها", ProductHeaders, NoLists);

    public static byte[] Prices() => Build("قیمت‌ها", PriceHeaders, NoLists);

    public static byte[] Parties() => Build("طرف حساب‌ها", PartyHeaders, NoLists);

    public static byte[] Categories() => Build("دسته‌بندی‌ها", CategoryHeaders, NoLists);

    // ================= قالب‌های پویا (با فهرست سیستم) =================
    public static async Task<byte[]> ProductsAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        return Build("کالاها", ProductHeaders, new (string[], IEnumerable<string[]>)[]
        {
            (new[] { "دسته‌بندی — فقط از این فهرست انتخاب کن" }, categories.Select(c => new[] { c }).ToList()),
            (new[] { "واحد — خالی بگذاری «عدد» می‌شود" }, Units.Select(u => new[] { u }).ToList())
        });
    }

    public static async Task<byte[]> PricesAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var items = await db.Products
            .Where(p => p.Code != null)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Code, p.Name })
            .ToListAsync();

        return Build("قیمت‌ها", PriceHeaders, new (string[], IEnumerable<string[]>)[]
        {
            (new[] { "کد", "نام" }, items.Select(p => new[] { p.Code, p.Name }).ToList())
        });
    }

    public static async Task<byte[]> PartiesAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var items = await db.Parties
            .OrderBy(p => p.Name)
            .Select(p => new { p.Code, p.Name })
            .ToListAsync();

        return Build("طرف حساب‌ها", PartyHeaders, new (string[], IEnumerable<string[]>)[]
        {
            (new[] { "نوع — فقط یکی از این سه" }, PartyTypes.Select(t => new[] { t }).ToList()),
            (new[] { "کد", "نام" }, items.Select(p => new[] { p.Code, p.Name }).ToList())
        });
    }

    public static async Task<byte[]> CategoriesAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var names = await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        return Build("دسته‌بندی‌ها", CategoryHeaders, new (string[], IEnumerable<string[]>)[]
        {
            (new[] { "دسته‌های موجود — برای ستون دستهٔ مادر" }, names.Select(n => new[] { n }).ToList())
        });
    }

    // ================= ساخت یک‌گذری فایل =================
    private static byte[] Build(string sheetName, string[] headers,
        IEnumerable<(string[] Titles, IEnumerable<string[]> Rows)> lists)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Column(i + 1).Width = Math.Max(14, headers[i].Length + 6);
        }

        sheet.SheetView.FreezeRows(1);

        var listSheet = workbook.Worksheets.Add("فهرست‌ها");
        var column = 1;
        var hasLists = false;

        foreach (var (titles, rows) in lists)
        {
            hasLists = true;

            for (var i = 0; i < titles.Length; i++)
            {
                var head = listSheet.Cell(1, column + i);
                head.Value = titles[i];
                head.Style.Font.Bold = true;
                head.Style.Fill.BackgroundColor = XLColor.LightGray;
                listSheet.Column(column + i).Width = 26;
            }

            var row = 2;
            foreach (var values in rows)
            {
                for (var i = 0; i < titles.Length && i < values.Length; i++)
                {
                    var cell = listSheet.Cell(row, column + i);
                    cell.Value = values[i] ?? string.Empty;
                    cell.Style.NumberFormat.Format = "@";
                }

                row++;
            }

            column += titles.Length + 1;
        }

        if (!hasLists)
        {
            workbook.Worksheets.Delete("فهرست‌ها");
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }
}