namespace WarehouseApp.Data;

/// <summary>
/// کانال فروش — پاسخ به این پرسش که «کالا کجا فروخته شد؟»
/// حضوری، تلفنی، دیوار، اینستاگرام، دیجی‌کالا و هر جای دیگر.
///
/// به‌عمد جدول است و نه فهرست ثابت؛ هر وقت کانال تازه‌ای اضافه شد،
/// فقط یک سطر به این جدول اضافه می‌شود و نیازی به تغییر برنامه نیست.
/// </summary>
public class SalesChannel
{
    public int Id { get; set; }

    /// <summary>نام کانال، مثل «دیوار» یا «اینستاگرام»</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>توضیح کوتاه برای راهنما</summary>
    public string? Hint { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
