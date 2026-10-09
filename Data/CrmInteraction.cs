using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>
/// نوع تعامل با طرف حساب
/// </summary>
public enum InteractionKind
{
    /// <summary>تماس تلفنی</summary>
    Call = 1,

    /// <summary>جلسه</summary>
    Meeting = 2,

    /// <summary>ایمیل</summary>
    Email = 3,

    /// <summary>پیامک</summary>
    Sms = 4,

    /// <summary>بازدید حضوری</summary>
    Visit = 5,

    /// <summary>یادداشت داخلی</summary>
    Note = 6,

    /// <summary>شکایت یا پیگیری مشکل</summary>
    Complaint = 7
}

/// <summary>
/// وضعیت پیگیری
/// </summary>
public enum InteractionStatus
{
    /// <summary>در انتظار انجام</summary>
    Open = 1,

    /// <summary>انجام شد</summary>
    Done = 2,

    /// <summary>لغو شد</summary>
    Canceled = 3
}

/// <summary>
/// یک پیگیری (تعامل) با طرف حساب.
/// هر تماس، جلسه یا یادداشت یک رکورد اینجا می‌سازد تا پروندهٔ طرف حساب کامل شود.
/// </summary>
public class CrmInteraction
{
    public int Id { get; set; }

    public int PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>نوع تعامل</summary>
    public InteractionKind Kind { get; set; } = InteractionKind.Call;

    /// <summary>موضوع</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>شرح گفتگو یا نتیجه</summary>
    public string? Notes { get; set; }

    /// <summary>زمان انجام تعامل</summary>
    public DateTime At { get; set; } = DateTime.Now;

    /// <summary>سررسید پیگیری بعدی (اختیاری)</summary>
    public DateTime? FollowUpAt { get; set; }

    /// <summary>وضعیت</summary>
    public InteractionStatus Status { get; set; } = InteractionStatus.Open;

    /// <summary>زمان انجام‌شدن</summary>
    public DateTime? DoneAt { get; set; }

    /// <summary>کارمند ثبت‌کننده</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>عنوان فارسی نوع — ذخیره نمی‌شود</summary>
    [NotMapped]
    public string KindTitle => Kind switch
    {
        InteractionKind.Call => "تماس تلفنی",
        InteractionKind.Meeting => "جلسه",
        InteractionKind.Email => "ایمیل",
        InteractionKind.Sms => "پیامک",
        InteractionKind.Visit => "بازدید",
        InteractionKind.Note => "یادداشت",
        _ => "شکایت"
    };

    /// <summary>نامکون (آیکون) نوع</summary>
    [NotMapped]
    public string KindIcon => Kind switch
    {
        InteractionKind.Call => "bi-telephone",
        InteractionKind.Meeting => "bi-people",
        InteractionKind.Email => "bi-envelope",
        InteractionKind.Sms => "bi-chat-dots",
        InteractionKind.Visit => "bi-geo-alt",
        InteractionKind.Note => "bi-journal-text",
        _ => "bi-exclamation-triangle"
    };

    /// <summary>عنوان فارسی وضعیت</summary>
    [NotMapped]
    public string StatusTitle => Status switch
    {
        InteractionStatus.Open => "در انتظار",
        InteractionStatus.Done => "انجام شد",
        _ => "لغو شد"
    };

    /// <summary>آیا سررسید گذشته و هنوز انجام نشده؟</summary>
    [NotMapped]
    public bool IsOverdue
        => Status == InteractionStatus.Open
           && FollowUpAt.HasValue
           && FollowUpAt.Value.Date < DateTime.Today;

    /// <summary>سررسید امروز است؟</summary>
    [NotMapped]
    public bool IsDueToday
        => Status == InteractionStatus.Open
           && FollowUpAt.HasValue
           && FollowUpAt.Value.Date == DateTime.Today;
}
