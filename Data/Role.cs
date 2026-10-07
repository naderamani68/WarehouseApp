namespace WarehouseApp.Data;

/// <summary>
/// نقش (سمت) — مجموعه‌ای از اختیارات که مدیر سیستم تعیین می‌کند.
/// نقش‌ها در پایگاه داده هستند، پس مدیر می‌تواند نقش جدید بسازد یا اختیارات را عوض کند.
/// </summary>
public class Role
{
    /// <summary>شناسهٔ نقش پیش‌فرض «مدیر سیستم» که هنگام راه‌اندازی اولیه استفاده می‌شود</summary>
    public const int AdminRoleId = 1;

    public int Id { get; set; }

    /// <summary>نام نقش، مثلاً «انباردار»</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>توضیح نقش</summary>
    public string? Description { get; set; }

    /// <summary>اختیارات این نقش</summary>
    public Permissions Permissions { get; set; } = Permissions.None;

    /// <summary>فعال یا غیرفعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>نقش پیش‌فرض سیستم — قابل حذف نیست</summary>
    public bool IsSystemRole { get; set; }

    /// <summary>کارمندانی که این نقش را دارند</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
