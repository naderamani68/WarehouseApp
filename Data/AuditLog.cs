namespace WarehouseApp.Data;

/// <summary>
/// تاریخچهٔ عملیات — ثبت اینکه چه کسی، چه وقت، چه چیزی را تغییر داده است
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>زمان انجام عملیات</summary>
    public DateTime At { get; set; } = DateTime.Now;

    /// <summary>نام کاربری انجام‌دهنده</summary>
    public string? Username { get; set; }

    /// <summary>نوع عملیات: افزودن / ویرایش / حذف / ورود / خروج</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>موضوع عملیات: محصول / انبار / دسته / حرکت / کارمند</summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>شناسهٔ رکورد تغییر‌یافته (اختیاری)</summary>
    public int? EntityId { get; set; }

    /// <summary>توضیح بیشتر</summary>
    public string? Details { get; set; }
}
