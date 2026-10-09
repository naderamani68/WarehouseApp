using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>دستهٔ هزینه — لیست آمادهٔ ثابت</summary>
public enum ExpenseCategory
{
    [Display(Name = "خرید کالا")] ProductPurchase = 1,
    [Display(Name = "خرید خدمات")] Service = 2,
    [Display(Name = "حمل و نقل")] Shipping = 3,
    [Display(Name = "اجاره")] Rent = 4,
    [Display(Name = "حقوق و دستمزد")] Salary = 5,
    [Display(Name = "مالیات و عوارض")] Tax = 6,
    [Display(Name = "تعمیرات و نگهداری")] Repair = 7,
    [Display(Name = "آب و برق و گاز")] Utilities = 8,
    [Display(Name = "متفرقه")] Misc = 9
}

/// <summary>روش پرداخت هزینه</summary>
public enum PaymentMethod
{
    [Display(Name = "نقد")] Cash = 1,
    [Display(Name = "کارت بانکی")] Card = 2,
    [Display(Name = "حواله / انتقال")] Transfer = 3,
    [Display(Name = "چک")] Cheque = 4,
    [Display(Name = "سایر")] Other = 5
}

/// <summary>هزینه — خرید کالا، خدمات، اجاره و ...</summary>
public class Expense
{
    public int Id { get; set; }

    public ExpenseCategory Category { get; set; } = ExpenseCategory.ProductPurchase;

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    /// <summary>شرح هزینه</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>تاریخ هزینه</summary>
    public DateTime At { get; set; } = DateTime.Today;

    /// <summary>طرف حساب (فروشنده یا پیمانکار) — اختیاری</summary>
    public int? PartyId { get; set; }

    public Party? Party { get; set; }

    /// <summary>کالای مرتبط — برای «هزینهٔ هر کالا»</summary>
    public int? ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>تعداد کالا در این هزینه (اختیاری)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Quantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ===== ویژگی‌های محاسباتی =====

    [NotMapped]
    public string CategoryTitle => Category switch
    {
        ExpenseCategory.ProductPurchase => "خرید کالا",
        ExpenseCategory.Service => "خرید خدمات",
        ExpenseCategory.Shipping => "حمل و نقل",
        ExpenseCategory.Rent => "اجاره",
        ExpenseCategory.Salary => "حقوق و دستمزد",
        ExpenseCategory.Tax => "مالیات و عوارض",
        ExpenseCategory.Repair => "تعمیرات و نگهداری",
        ExpenseCategory.Utilities => "آب و برق و گاز",
        _ => "متفرقه"
    };

    [NotMapped]
    public string CategoryIcon => Category switch
    {
        ExpenseCategory.ProductPurchase => "bi-box-seam",
        ExpenseCategory.Service => "bi-tools",
        ExpenseCategory.Shipping => "bi-truck",
        ExpenseCategory.Rent => "bi-house-door",
        ExpenseCategory.Salary => "bi-people",
        ExpenseCategory.Tax => "bi-receipt",
        ExpenseCategory.Repair => "bi-wrench-adjustable",
        ExpenseCategory.Utilities => "bi-lightning-charge",
        _ => "bi-three-dots"
    };

    [NotMapped]
    public string CategoryBadge => Category switch
    {
        ExpenseCategory.ProductPurchase => "bg-primary",
        ExpenseCategory.Service => "bg-info text-dark",
        ExpenseCategory.Shipping => "bg-secondary",
        ExpenseCategory.Rent => "bg-dark",
        ExpenseCategory.Salary => "bg-success",
        ExpenseCategory.Tax => "bg-danger",
        ExpenseCategory.Repair => "bg-warning text-dark",
        ExpenseCategory.Utilities => "bg-light text-dark",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string MethodTitle => Method switch
    {
        PaymentMethod.Cash => "نقد",
        PaymentMethod.Card => "کارت بانکی",
        PaymentMethod.Transfer => "حواله / انتقال",
        PaymentMethod.Cheque => "چک",
        _ => "سایر"
    };

    /// <summary>بهای واحد = مبلغ تقسیم بر تعداد</summary>
    [NotMapped]
    public decimal? UnitCost => Quantity is > 0 ? Math.Round(Amount / Quantity.Value, 0) : null;
}
