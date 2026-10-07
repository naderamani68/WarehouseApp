using System.Globalization;
using ClosedXML.Excel;

namespace WarehouseApp.Services;

public class ExcelService
{
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
        foreach (var item in dataList)
        {
            col = 1;
            foreach (var c in columns)
            {
                var val = c.Value(item);
                var cell = ws.Cell(row, col);

                cell.Value = val switch
                {
                    null => string.Empty,
                    DateTime dt => usePersianDate
                        ? FormatPersianDate(dt)
                        : dt.ToString("yyyy/MM/dd HH:mm"),
                    _ => val.ToString()
                };

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

        // ===== راست‌چین کردن کل شیت =====
        ws.RightToLeft = true;

        // ===== فریز کردن سطر هدر (وقتی اسکرول می‌کنی، هدر ثابت بمونه) =====
        ws.SheetView.FreezeRows(headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string FormatPersianDate(DateTime dt)
    {
        return $"{_pc.GetYear(dt):0000}/{_pc.GetMonth(dt):00}/{_pc.GetDayOfMonth(dt):00} {dt:HH:mm}";
    }
}