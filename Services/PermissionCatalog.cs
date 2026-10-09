using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>یک اختیار برای نمایش در جدول دسترسی‌ها</summary>
public record PermissionItem(Permissions Value, string Title, string Group);

/// <summary>
/// فهرست اختیارات همراه با عنوان فارسی و گروه — برای ساخت جدول تیک‌زدنی
/// </summary>
public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionItem> All { get; } = new List<PermissionItem>
    {
        new(Permissions.ViewData,          "دیدن اطلاعات انبار",         "عمومی"),
        new(Permissions.ManageProducts,    "افزودن و ویرایش محصول",      "محصولات"),
        new(Permissions.DeleteProducts,    "حذف محصول",                  "محصولات"),
        new(Permissions.ManageCategories,  "افزودن و ویرایش دسته‌بندی",  "دسته‌بندی‌ها"),
        new(Permissions.DeleteCategories,  "حذف دسته‌بندی",              "دسته‌بندی‌ها"),
        new(Permissions.ManageWarehouses,  "افزودن و ویرایش انبار",      "انبارها"),
        new(Permissions.DeleteWarehouses,  "حذف انبار",                  "انبارها"),
        new(Permissions.RegisterMovements, "ثبت حرکات انبار",            "عملیات انبار"),
        new(Permissions.ExportExcel,       "خروجی اکسل",                 "گزارش‌ها"),
        new(Permissions.ViewReports,       "دیدن گزارش‌ها",              "گزارش‌ها"),
        new(Permissions.ViewAuditLog,      "دیدن تاریخچهٔ عملیات",       "مدیریت سیستم"),
        new(Permissions.ViewParties,       "دیدن مشتریان و تأمین‌کنندگان",  "مشتریان و تأمین‌کنندگان"),
        new(Permissions.ManageParties,     "افزودن و ویرایش طرف حساب",      "مشتریان و تأمین‌کنندگان"),
        new(Permissions.DeleteParties,     "حذف طرف حساب",                  "مشتریان و تأمین‌کنندگان"),
        new(Permissions.ViewCrm,           "دیدن پیگیری‌ها، فرصت‌ها و سفارش‌ها", "فروش و پیگیری (CRM)"),
        new(Permissions.ManageCrm,         "افزودن و ویرایش پیگیری، فرصت و سفارش", "فروش و پیگیری (CRM)"),
        new(Permissions.DeleteCrm,         "حذف پیگیری، فرصت و سفارش",     "فروش و پیگیری (CRM)"),
        new(Permissions.ConfirmSalesOrders,"تأیید سفارش فروش و کسر از انبار", "فروش و پیگیری (CRM)"),
        new(Permissions.ManageEmployees,   "مدیریت کارمندان",            "مدیریت سیستم"),
        new(Permissions.ManageRoles,       "مدیریت نقش‌ها و دسترسی‌ها",  "مدیریت سیستم")
    };

    /// <summary>گروه‌های اختیارات، به ترتیب نمایش</summary>
    public static IReadOnlyList<string> Groups { get; } =
        All.Select(p => p.Group).Distinct().ToList();
}
