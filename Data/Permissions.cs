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

    /// <summary>دیدن مشتریان و تأمین‌کنندگان</summary>
    ViewParties = 1 << 13,

    /// <summary>افزودن و ویرایش مشتریان و تأمین‌کنندگان</summary>
    ManageParties = 1 << 14,

    /// <summary>حذف مشتریان و تأمین‌کنندگان</summary>
    DeleteParties = 1 << 15,

    /// <summary>دیدن پیگیری‌ها، فرصت‌های فروش و سفارش‌های فروش</summary>
    ViewCrm = 1 << 16,

    /// <summary>افزودن و ویرایش پیگیری، فرصت فروش و سفارش فروش</summary>
    ManageCrm = 1 << 17,

    /// <summary>حذف پیگیری، فرصت فروش و سفارش فروش</summary>
    DeleteCrm = 1 << 18,

    /// <summary>تأیید سفارش فروش (کسر واقعی از انبار)</summary>
    ConfirmSalesOrders = 1 << 19,

    /// <summary>دیدن وضعیت مالی (چک، سفته، بدهی، طلب و هزینه‌ها)</summary>
    ViewFinance = 1 << 20,

    /// <summary>افزودن و ویرایش سند مالی و هزینه</summary>
    ManageFinance = 1 << 21,

    /// <summary>حذف سند مالی و هزینه</summary>
    DeleteFinance = 1 << 22,

    /// <summary>مدیریت تنظیمات سیستم (نرخ مالیات، اطلاعات شرکت)</summary>
    ManageSettings = 1 << 23,

    /// <summary>پنل فروشندگان — دیدن سفارش‌های خودِ فروشنده</summary>
    SalesPanel = 1 << 24,

    /// <summary>همهٔ اختیارات</summary>
    All = ViewData | ManageProducts | DeleteProducts | ManageCategories | DeleteCategories
        | ManageWarehouses | DeleteWarehouses | RegisterMovements | ExportExcel
        | ViewReports | ManageEmployees | ManageRoles | ViewAuditLog
        | ViewParties | ManageParties | DeleteParties
        | ViewCrm | ManageCrm | DeleteCrm | ConfirmSalesOrders
        | ViewFinance | ManageFinance | DeleteFinance | ManageSettings | SalesPanel
}
