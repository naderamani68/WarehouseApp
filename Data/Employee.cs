using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>
/// کارمند (هم مشخصات کارمندی و هم اطلاعات ورود به سیستم)
/// </summary>
public class Employee
{
    public int Id { get; set; }

    // ===== مشخصات کارمندی =====
    public string EmployeeCode { get; set; } = string.Empty;   // کد پرسنلی
    public string FullName { get; set; } = string.Empty;       // نام و نام خانوادگی
    public string? NationalId { get; set; }                     // کد ملی
    public string? Phone { get; set; }                          // تلفن
    public string? Address { get; set; }                        // آدرس
    public string? Position { get; set; }                       // سمت
    public DateTime? HireDate { get; set; }                     // تاریخ استخدام
    public bool IsActive { get; set; } = true;                  // فعال / غیرفعال

    // ===== اطلاعات ورود =====
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string PasswordHash { get; set; } = string.Empty;    // هش رمز عبور
    public int? RoleId { get; set; }                            // نقش (سمت)
    public Role? Role { get; set; }                             // ارتباط با جدول نقش‌ها
    public bool MustChangePassword { get; set; }                // تغییر رمز در اولین ورود
    public DateTime? LastLoginAt { get; set; }                  // آخرین ورود
    public int FailedLoginCount { get; set; }                   // تعداد تلاش‌های ناموفق
    public DateTime? LockoutUntil { get; set; }                 // قفل موقت تا این زمان

    /// <summary>عنوان فارسی نقش — در پایگاه داده ذخیره نمی‌شود</summary>
    /// <summary>نام نقش — در پایگاه داده ذخیره نمی‌شود</summary>
    [NotMapped]
    public string RoleTitle => Role?.Name ?? "بدون نقش";
}
