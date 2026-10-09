using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>نوع بدهی: بدهی ما به دیگران یا طلب ما از دیگران</summary>
public enum DebtType
{
    [Display(Name = "بدهی (ما بدهکاریم)")] Payable = 1,
    [Display(Name = "طلب (ما طلبکاریم)")] Receivable = 2
}

public enum DebtStatus
{
    [Display(Name = "باز")] Open = 1,
    [Display(Name = "تسویه‌شده")] Settled = 2
}

/// <summary>بدهی یا طلب — با امکان پرداخت جزئی</summary>
public class Debt
{
    public int Id { get; set; }

    public DebtType Type { get; set; } = DebtType.Payable;

    /// <summary>طرف حساب مرتبط (اختیاری)</summary>
    public int? PartyId { get; set; }

    public Party? Party { get; set; }

    /// <summary>عنوان بدهی یا طلب</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>مبلغ پرداخت‌شده تا این لحظه</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    /// <summary>تاریخ ایجاد بدهی</summary>
    public DateTime IssueDate { get; set; } = DateTime.Today;

    /// <summary>سررسید (اختیاری)</summary>
    public DateTime? DueDate { get; set; }

    public DebtStatus Status { get; set; } = DebtStatus.Open;

    public DateTime? SettledAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ===== ویژگی‌های محاسباتی =====

    [NotMapped]
    public decimal Remaining => Math.Max(0m, Amount - PaidAmount);

    [NotMapped]
    public decimal PaidPercent => Amount <= 0 ? 0m : Math.Min(100m, Math.Round(PaidAmount / Amount * 100m, 0));

    [NotMapped]
    public bool IsSettled => Status == DebtStatus.Settled || Remaining <= 0;

    [NotMapped]
    public string TypeTitle => Type == DebtType.Payable ? "بدهی" : "طلب";

    [NotMapped]
    public string StatusTitle => IsSettled ? "تسویه‌شده" : "باز";

    [NotMapped]
    public string StatusBadge => IsSettled ? "bg-success" : "bg-warning text-dark";

    [NotMapped]
    public string TypeBadge => Type == DebtType.Payable ? "bg-danger" : "bg-success";

    [NotMapped]
    public bool IsOverdue => !IsSettled && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

    /// <summary>مبلغ با علامت: بدهی منفی، طلب مثبت</summary>
    [NotMapped]
    public decimal SignedRemaining => Type == DebtType.Payable ? -Remaining : Remaining;
}
