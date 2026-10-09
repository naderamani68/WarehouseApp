namespace WarehouseApp.Services;

/// <summary>
/// ثابت‌های احراز هویت فروشگاه — کاملاً جدا از کارمندان انبار.
/// مشتری فروشگاه با یک کوکی جداگانه وارد می‌شود که روی مسیر فروشگاه محدود است؛
/// پس هرگز به پنل انبار فرستاده نمی‌شود و بالعکس.
/// </summary>
public static class ShopAuth
{
    /// <summary>نام طرح ورود مشتریان فروشگاه</summary>
    public const string Scheme = "ShopCustomer";

    /// <summary>نام سیاست دسترسی مخصوص مشتریان فروشگاه</summary>
    public const string CustomerPolicy = "ShopCustomerOnly";
}
