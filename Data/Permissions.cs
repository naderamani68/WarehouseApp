namespace WarehouseApp.Data;

/// <summary>
/// اختیارات دسترسی.
/// هر اختیار یک «بیت» است تا بتوان چند اختیار را در یک عدد ذخیره کرد.
/// همین باعث می‌شود جدول نقش‌ها ساده بماند و مدیر بتواند ترکیب دلخواه بسازد.
/// </summary>
[Flags]
public enum Permissions
{
    None = 0,

    /// <summary>دیدن اطلاعات (محصولات، دسته‌ها، انبارها، حرکات)</summary>
    ViewData = 1 << 0,

    /// <summary>افزودن و ویرایش محصول</summary>
    ManageProducts = 1 << 1,

    /// <summary>حذف محصول</summary>
    DeleteProducts = 1 << 2,

    /// <summary>افزودن و ویرایش دسته‌بندی</summary>
    ManageCategories = 1 << 3,

    /// <summary>حذف دسته‌بندی</summary>
    DeleteCategories = 1 << 4,

    /// <summary>افزودن و ویرایش انبار</summary>
    ManageWarehouses = 1 << 5,

    /// <summary>حذف انبار</summary>
    DeleteWarehouses = 1 << 6,

    /// <summary>ثبت حرکات انبار (ورود و خروج)</summary>
    RegisterMovements = 1 << 7,

    /// <summary>گرفتن خروجی اکسل</summary>
    ExportExcel = 1 << 8,

    /// <summary>دیدن گزارش‌ها</summary>
    ViewReports = 1 << 9,

    /// <summary>مدیریت کارمندان</summary>
    ManageEmployees = 1 << 10,

    /// <summary>مدیریت نقش‌ها و دسترسی‌ها</summary>
    ManageRoles = 1 << 11,

    /// <summary>دیدن تاریخچهٔ عملیات</summary>
    ViewAuditLog = 1 << 12,

    /// <summary>همهٔ اختیارات</summary>
    All = ViewData | ManageProducts | DeleteProducts | ManageCategories | DeleteCategories
        | ManageWarehouses | DeleteWarehouses | RegisterMovements | ExportExcel
        | ViewReports | ManageEmployees | ManageRoles | ViewAuditLog
}
