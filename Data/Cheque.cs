using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>نوع سند مالی: چک یا سفته</summary>
public enum ChequeKind
{
    [Display(Name = "چک")] Cheque = 1,
    [Display(Name = "سفته")] PromissoryNote = 2
}

/// <summary>جهت سند: دریافتی (از مشتری) یا پرداختی (به تأمین‌کننده)</summary>
public enum ChequeDirection
{
    [Display(Name = "دریافتی")] Incoming = 1,
    [Display(Name = "پرداختی")] Outgoing = 2
}

/// <summary>وضعیت سند</summary>
public enum ChequeStatus
{
    [Display(Name = "در جریان")] Pending = 1,
    [Display(Name = "وصول / پاس شده")] Cleared = 2,
    [Display(Name = "برگشتی")] Returned = 3,
    [Display(Name = "باطل / خرج شده")] Canceled = 4
}

/// <summary>چک یا سفته — دریافتی یا پرداختی</summary>
public class Cheque
{
    public int Id { get; set; }

    public ChequeKind Kind { get; set; } = ChequeKind.Cheque;

    public ChequeDirection Direction { get; set; } = ChequeDirection.Incoming;

    /// <summary>شمارهٔ چک یا سفته</summary>
    [MaxLength(50)]
    public string? Serial { get; set; }

    /// <summary>نام بانک یا صادرکننده</summary>
    [MaxLength(100)]
    public string? BankName { get; set; }

    /// <summary>طرف حساب مرتبط (اختیاری)</summary>
    public int? PartyId { get; set; }

    public Party? Party { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>تاریخ صدور</summary>
    public DateTime IssueDate { get; set; } = DateTime.Today;

    /// <summary>تاریخ سررسید</summary>
    public DateTime DueDate { get; set; } = DateTime.Today;

    public ChequeStatus Status { get; set; } = ChequeStatus.Pending;

    /// <summary>تاریخ وصول یا برگشت</summary>
    public DateTime? ClosedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ===== ویژگی‌های محاسباتی (در پایگاه داده ذخیره نمی‌شوند) =====

    [NotMapped]
    public string KindTitle => Kind == ChequeKind.Cheque ? "چک" : "سفته";

    [NotMapped]
    public string DirectionTitle => Direction == ChequeDirection.Incoming ? "دریافتی" : "پرداختی";

    [NotMapped]
    public string StatusTitle => Status switch
    {
        ChequeStatus.Pending => "در جریان",
        ChequeStatus.Cleared => "وصول / پاس شده",
        ChequeStatus.Returned => "برگشتی",
        _ => "باطل / خرج شده"
    };

    [NotMapped]
    public string StatusBadge => Status switch
    {
        ChequeStatus.Pending => "bg-warning text-dark",
        ChequeStatus.Cleared => "bg-success",
        ChequeStatus.Returned => "bg-danger",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string DirectionBadge => Direction == ChequeDirection.Incoming ? "bg-success" : "bg-danger";

    /// <summary>مبلغ با علامت: دریافتی مثبت، پرداختی منفی</summary>
    [NotMapped]
    public decimal SignedAmount => Direction == ChequeDirection.Incoming ? Amount : -Amount;

    /// <summary>آیا سررسید گذشته و هنوز در جریان است؟</summary>
    [NotMapped]
    public bool IsOverdue => Status == ChequeStatus.Pending && DueDate.Date < DateTime.Today;

    [NotMapped]
    public bool IsDueToday => Status == ChequeStatus.Pending && DueDate.Date == DateTime.Today;

    /// <summary>روزهای باقی‌مانده تا سررسید (منفی = گذشته)</summary>
    [NotMapped]
    public int DaysLeft => (DueDate.Date - DateTime.Today).Days;
}
