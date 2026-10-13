using System.Globalization;
using ClosedXML.Excel;

namespace WarehouseApp.Services;

public class ExcelService
{
    /// <summary>
    /// فایل بارگذاری‌شدهٔ مرورگر را به‌صورت ناهم‌زمان در حافظه می‌خواند.
    /// چرا؟ جریان فایل مرورگر «خواندن هم‌زمان» را پشتیبانی نمی‌کند و کتابخانهٔ اکسل هم‌زمان می‌خواند.
    /// </summary>
    public static async Task<MemoryStream> CopyUploadAsync(Stream source)
    {
        var buffer = new MemoryStream();

        await using (source)
        {
            await source.CopyToAsync(buffer);
        }

        buffer.Position = 0;
        return buffer;
    }
    private static readonly PersianCalendar _pc = new();

    // فونت پیشنهادی برای نمایش صحیح فارسی
    private const string PersianFont = "Tahoma";

    public byte[] Export<T>(
        IEnumerable<T> data,
        string sheetName,
        Dictionary<string, Func<T, object?>> columns,
        string title = "",
        bool usePersianDate = true)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        var dataList = data.ToList();
        int row = 1;

        // ===== عنوان (اختیاری) =====
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleRange = ws.Range(1, 1, 1, columns.Count);
            titleRange.Merge();
            titleRange.Value = title;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 13;
            titleRange.Style.Font.FontName = PersianFont;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            titleRange.Style.Alignment.WrapText = true;
            ws.Row(1).Height = 45;
            row = 3;
        }

        // ===== هدر =====
        int col = 1;
        foreach (var c in columns)
        {
            var cell = ws.Cell(row, col);
            cell.Value = c.Key;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontName = PersianFont;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            col++;
        }

        int headerRow = row;
        ws.Row(headerRow).Height = 28;
        row++;

        // ===== داده‌ها =====
        var numericColumns = new HashSet<int>();

        foreach (var item in dataList)
        {
            col = 1;
            foreach (var c in columns)
            {
                var val = c.Value(item);
                var cell = ws.Cell(row, col);

                var text = val switch
                {
                    null => string.Empty,
                    DateTime dt => usePersianDate
                        ? FormatPersianDate(dt)
                        : dt.ToString("yyyy/MM/dd HH:mm"),
                    _ => val.ToString() ?? string.Empty
                };

                // عددها به‌صورت عدد واقعی نوشته می‌شوند تا در اکسل قابل جمع زدن باشند
                if (TryNumber(text, out var number))
                {
                    cell.Value = number;
                    cell.Style.NumberFormat.Format = "#,##0";
                    numericColumns.Add(col);
                }
                else
                {
                    cell.Value = text;
                }

                cell.Style.Font.FontName = PersianFont;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = false;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // حاشیه‌ی نازک
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                cell.Style.Border.LeftBorder = XLBorderStyleValues.Hair;
                cell.Style.Border.RightBorder = XLBorderStyleValues.Hair;

                col++;
            }
            row++;
        }

        // ===== تنظیم عرض ستون‌ها =====
        // ۱) اول بر اساس محتوا محاسبه کن
        try
        {
            ws.Columns(1, columns.Count).AdjustToContents();
        }
        catch
        {
            // اگه به هر دلیلی خطا داد، بی‌خیال شو
        }

        // ۲) بعد حداقل/حداکثر عرض رو اعمال کن
        for (int i = 1; i <= columns.Count; i++)
        {
            var column = ws.Column(i);

            // حداقل عرض برای متن فارسی
            if (column.Width < 14) column.Width = 14;

            // حداکثر عرض (تا یه ستون کل صفحه رو نگیره)
            if (column.Width > 50) column.Width = 50;
        }

        // ۳) مقدار کمی padding به همه‌ی ستون‌ها اضافه کن
        //    (چون AdjustToContents برای فارسی معمولاً کم‌برآورد می‌کنه)
        for (int i = 1; i <= columns.Count; i++)
        {
            var column = ws.Column(i);
            if (column.Width < 50)
            {
                column.Width = Math.Min(column.Width * 1.15, 50);
            }
        }

        // ===== ارتفاع سطرها =====
        for (int r = headerRow + 1; r < row; r++)
        {
            ws.Row(r).Height = 20;
        }

        // ===== فیلتر خودکار =====
        if (dataList.Count > 0)
        {
            ws.Range(headerRow, 1, row - 1, columns.Count).SetAutoFilter();
        }

        // ===== ردیف جمع کل =====
        if (numericColumns.Count > 0 && dataList.Count > 1)
        {
            int totalRow = row;
            int labelCol = Enumerable.Range(1, columns.Count).FirstOrDefault(i => !numericColumns.Contains(i));

            for (int i = 1; i <= columns.Count; i++)
            {
                var cell = ws.Cell(totalRow, i);
                cell.Style.Font.FontName = PersianFont;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (i == labelCol)
                {
                    cell.Value = "جمع کل";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (numericColumns.Contains(i))
                {
                    var letter = ws.Column(i).ColumnLetter();
                    cell.FormulaA1 = $"SUM({letter}{headerRow + 1}:{letter}{row - 1})";
                    cell.Style.NumberFormat.Format = "#,##0";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }

            ws.Row(totalRow).Height = 22;
        }

        // ===== راست‌چین کردن کل شیت =====
        ws.RightToLeft = true;

        // ===== فریز کردن سطر هدر (وقتی اسکرول می‌کنی، هدر ثابت بمونه) =====
        ws.SheetView.FreezeRows(headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// تشخیص متن عددی: مبلغ با جداکنندهٔ هزار (۱٬۲۳۴ یا 1,234) یا عدد صحیح کوتاه.
    /// شمارهٔ موبایل و کد ملی عمداً عدد نمی‌شوند تا صفر اولشان از دست نرود.
    /// </summary>
    private static bool TryNumber(string text, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var clean = NormalizeDigits(text.Trim());

        // مبلغ با جداکنندهٔ هزار
        if (System.Text.RegularExpressions.Regex.IsMatch(clean, @"^-?\d{1,3}(,\d{3})+$"))
        {
            return decimal.TryParse(clean.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // عدد صحیح کوتاه (تعداد، سال) — بدون صفر ابتدایی و بیشتر از ۹ رقم نباشد
        if (System.Text.RegularExpressions.Regex.IsMatch(clean, @"^-?[1-9]\d{0,8}$"))
        {
            return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }

    /// <summary>تبدیل رقم‌های فارسی و عربی به رقم لاتین</summary>
    private static string NormalizeDigits(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);

        foreach (var ch in text)
        {
            builder.Append(ch switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)(ch - '\u06F0' + '0'),
                >= '\u0660' and <= '\u0669' => (char)(ch - '\u0660' + '0'),
                '\u066B' => '.',
                '\u066C' => ',',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static string FormatPersianDate(DateTime dt)
    {
        return $"{_pc.GetYear(dt):0000}/{_pc.GetMonth(dt):00}/{_pc.GetDayOfMonth(dt):00} {dt:HH:mm}";
    }
}