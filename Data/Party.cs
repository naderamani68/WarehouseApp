using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>
/// نوع طرف حساب
/// </summary>
public enum PartyType
{
    /// <summary>مشتری</summary>
    Customer = 1,

    /// <summary>تأمین‌کننده</summary>
    Supplier = 2,

    /// <summary>هم مشتری و هم تأمین‌کننده</summary>
    Both = 3
}

/// <summary>
/// طرف حساب — مشتری، تأمین‌کننده یا هر دو.
/// هر دو در یک جدول نگه‌داری می‌شوند تا اطلاعات تکراری نشود.
/// </summary>
public class Party
{
    public int Id { get; set; }

    /// <summary>کد طرف حساب (یکتا)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>نام شخص یا شرکت</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>مشتری / تأمین‌کننده / هر دو</summary>
    public PartyType Type { get; set; } = PartyType.Customer;

    /// <summary>نام شخص رابط</summary>
    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }

    /// <summary>کد ملی (شخص) یا شناسهٔ ملی (شرکت)</summary>
    public string? NationalId { get; set; }

    /// <summary>کد اقتصادی</summary>
    public string? EconomicCode { get; set; }

    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>یادداشت آزاد</summary>
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>عنوان فارسی نوع — در پایگاه داده ذخیره نمی‌شود</summary>
    [NotMapped]
    public string TypeTitle => Type switch
    {
        PartyType.Customer => "مشتری",
        PartyType.Supplier => "تأمین‌کننده",
        _ => "مشتری و تأمین‌کننده"
    };
}
