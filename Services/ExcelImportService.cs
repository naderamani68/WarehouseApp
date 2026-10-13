using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>نتیجهٔ وارد کردن از اکسل</summary>
public record ImportResult(int Added, int Updated, int Skipped, List<string> Notes)
{
    /// <summary>متن خلاصه برای نمایش به کاربر</summary>
    public string Summary
    {
        get
        {
            var text = $"{Added} ردیف اضافه شد";

            if (Updated > 0)
            {
                text += $" و {Updated} ردیف به‌روز شد";
            }

            if (Skipped > 0)
            {
                text += $" و {Skipped} ردیف رد شد";
            }

            return text;
        }
    }
}

/// <summary>
/// وارد کردن داده از فایل اکسل.
/// ستون‌ها با ترتیب یا عنوان فارسی شناسایی می‌شوند و ردیف‌های ناقص با پیام روشن رد می‌شوند.
/// </summary>
public class ExcelImportService
{
    private const int MaxNotes = 15;

    private readonly ApplicationDbContext _db;

    public ExcelImportService(ApplicationDbContext db) => _db = db;

    // ===== طرف حساب‌ها =====

    /// <summary>
    /// ترتیب ستون‌ها: کد، نام، نوع، شخص رابط، تلفن، موبایل، ایمیل، کد ملی، کد اقتصادی، شهر، نشانی، کد پستی، یادداشت
    /// </summary>
    public async Task<ImportResult> ImportPartiesAsync(Stream stream)
    {
        var notes = new List<string>();
        var added = 0;
        var updated = 0;
        var skipped = 0;

        var sheet = OpenSheet(stream, notes);

        if (sheet is null)
        {
            return new ImportResult(0, 0, 0, notes);
        }

        var parties = await _db.Parties.ToListAsync();
        var nextNumber = NextNumber(parties.Select(p => p.Code));

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var line = row.RowNumber();
            var code = Text(row.Cell(1));
            var name = Text(row.Cell(2));

            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                Add(notes, $"ردیف {line}: نام خالی است");
                continue;
            }

            var mobile = Text(row.Cell(6));

            if (!string.IsNullOrWhiteSpace(mobile) && !Mobile.IsValid(mobile))
            {
                skipped++;
                Add(notes, $"ردیف {line}: موبایل «{mobile}» باید ۱۱ رقم و با ۰۹ شروع شود");
                continue;
            }

            var typeText = Text(row.Cell(3)).Trim();

            if (!TryParsePartyType(typeText, out var partyType))
            {
                skipped++;
                Add(notes, $"ردیف {row.RowNumber()}: نوع «{typeText}» شناخته نشد — یکی از مشتری، تأمین‌کننده، هر دو");
                continue;
            }

            var party = parties.FirstOrDefault(p => !string.IsNullOrWhiteSpace(code) && p.Code == code)
                        ?? parties.FirstOrDefault(p => p.Name == name);

            if (party is null)
            {
                party = new Party
                {
                    Code = string.IsNullOrWhiteSpace(code) ? $"P-{nextNumber:0000}" : code,
                    CreatedAt = DateTime.Now
                };

                nextNumber++;
                _db.Parties.Add(party);
                parties.Add(party);
                added++;
            }
            else
            {
                updated++;
            }

            party.Name = name;
            party.Type = partyType;
            party.ContactPerson = Nullable(Text(row.Cell(4)));
            party.Phone = Nullable(Text(row.Cell(5)));
            party.Mobile = string.IsNullOrWhiteSpace(mobile) ? null : Mobile.Normalize(mobile);
            party.Email = Nullable(Text(row.Cell(7)));
            party.NationalId = Nullable(Text(row.Cell(8)));
            party.EconomicCode = Nullable(Text(row.Cell(9)));
            party.City = Nullable(Text(row.Cell(10)));
            party.Address = Nullable(Text(row.Cell(11)));
            party.PostalCode = Nullable(Text(row.Cell(12)));
            party.Notes = Nullable(Text(row.Cell(13)));
        }

        await _db.SaveChangesAsync();
        return new ImportResult(added, updated, skipped, notes);
    }

    // ===== دسته‌بندی‌ها =====

    /// <summary>ترتیب ستون‌ها: نام دسته، دستهٔ مادر، توضیح</summary>
    public async Task<ImportResult> ImportCategoriesAsync(Stream stream)
    {
        var notes = new List<string>();
        var added = 0;
        var updated = 0;
        var skipped = 0;

        var sheet = OpenSheet(stream, notes);

        if (sheet is null)
        {
            return new ImportResult(0, 0, 0, notes);
        }

        var categories = await _db.Categories.ToListAsync();

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var line = row.RowNumber();
            var name = Text(row.Cell(1));
            var parentName = Text(row.Cell(2));

            if (string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(parentName) && parentName == name)
            {
                skipped++;
                Add(notes, $"ردیف {line}: دسته نمی‌تواند مادر خودش باشد");
                continue;
            }

            var parent = categories.FirstOrDefault(c => c.Name == parentName);

            if (!string.IsNullOrWhiteSpace(parentName) && parent is null)
            {
                skipped++;
                Add(notes, $"ردیف {row.RowNumber()}: دستهٔ مادر «{parentName}» در سیستم نیست — اول آن را بساز");
                continue;
            }

            var category = categories.FirstOrDefault(c => c.Name == name);

            if (category is null)
            {
                category = new Category { Name = name, IsActive = true };
                _db.Categories.Add(category);
                categories.Add(category);
                added++;
            }
            else
            {
                updated++;
            }

            category.Description = Nullable(Text(row.Cell(3)));
            category.ParentId = parent?.Id;
            category.Parent = parent;
        }

        await _db.SaveChangesAsync();
        return new ImportResult(added, updated, skipped, notes);
    }

    // ===== کمکی‌ها =====

    private static IXLWorksheet? OpenSheet(Stream stream, List<string> notes)
    {
        var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();

        if (sheet is null)
        {
            notes.Add("فایل اکسل هیچ برگه‌ای ندارد");
        }

        return sheet;
    }

    /// <summary>متن خانه — عددها هم به متن تبدیل می‌شوند تا کد کالا و موبایل از دست نروند</summary>
    private static string Text(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.DataType == XLDataType.Number
            ? cell.GetDouble().ToString("0.######", CultureInfo.InvariantCulture).Trim()
            : cell.GetString().Trim();
    }

    private static string? Nullable(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static void Add(List<string> notes, string text)
    {
        if (notes.Count < MaxNotes)
        {
            notes.Add(text);
        }
    }

    /// <summary>شمارهٔ بعدی برای کد خودکار طرف حساب</summary>
    private static int NextNumber(IEnumerable<string> codes)
    {
        var max = 0;

        foreach (var code in codes)
        {
            var digits = new string(code.Where(char.IsDigit).ToArray());

            if (int.TryParse(digits, out var value) && value > max)
            {
                max = value;
            }
        }

        return max + 1;
    }

    /// <summary>نوع طرف حساب از متن فارسی یا انگلیسی</summary>
    private static bool TryParsePartyType(string text, out PartyType type)
    {
        switch ((text ?? string.Empty).Trim())
        {
            case "":
            case "مشتری":
            case "Customer":
                type = PartyType.Customer;
                return true;

            case "تأمین‌کننده":
            case "تامین کننده":
            case "Supplier":
                type = PartyType.Supplier;
                return true;

            case "هر دو":
            case "مشتری و تأمین‌کننده":
            case "Both":
                type = PartyType.Both;
                return true;

            default:
                type = PartyType.Customer;
                return false;
        }
    }

    private static PartyType ParsePartyType(string text) => text.Trim() switch
    {
        "تأمین‌کننده" or "تامین کننده" or "Supplier" => PartyType.Supplier,
        "هر دو" or "مشتری و تأمین‌کننده" or "Both" => PartyType.Both,
        _ => PartyType.Customer
    };
}