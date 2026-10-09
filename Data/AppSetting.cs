namespace WarehouseApp.Data;

/// <summary>
/// تنظیمات برنامه به‌صورت «کلید / مقدار».
/// چرا در پایگاه داده و نه در کد؟ چون چیزهایی مثل نرخ مالیات با تغییر قانون عوض می‌شوند
/// و مدیر باید بتواند خودش، بدون برنامه‌نویس، مقدار را تغییر دهد.
/// </summary>
public class AppSetting
{
    /// <summary>کلید یکتا — مثل Tax.VatPercent</summary>
    [System.ComponentModel.DataAnnotations.Key]
    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string? Value { get; set; }

    /// <summary>عنوان فارسی برای نمایش در صفحهٔ تنظیمات</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>گروه‌بندی — مثل مالیات، اطلاعات شرکت</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string Group { get; set; } = "عمومی";

    /// <summary>نوع مقدار: text | number | percent | bool</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string ValueType { get; set; } = "text";

    /// <summary>توضیح کوتاه برای مدیر، زیر هر فیلد</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string? Hint { get; set; }

    public int SortOrder { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string? Username { get; set; }
}
