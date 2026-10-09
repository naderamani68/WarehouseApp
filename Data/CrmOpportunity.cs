using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>
/// مرحلهٔ فرصت فروش — قیف فروش
/// </summary>
public enum OpportunityStage
{
    /// <summary>فرصت تازه</summary>
    New = 1,

    /// <summary>تماس برقرار شد</summary>
    Contacted = 2,

    /// <summary>نیازسنجی شد</summary>
    Qualified = 3,

    /// <summary>پیشنهاد قیمت داده شد</summary>
    Proposal = 4,

    /// <summary>در حال مذاکره</summary>
    Negotiation = 5,

    /// <summary>برنده شد</summary>
    Won = 6,

    /// <summary>از دست رفت</summary>
    Lost = 7
}

/// <summary>
/// فرصت فروش — معاملهٔ در جریان با یک طرف حساب
/// </summary>
public class CrmOpportunity
{
    public int Id { get; set; }

    public int PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>عنوان معامله</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>مبلغ تخمینی</summary>
    public decimal Amount { get; set; }

    /// <summary>مرحلهٔ قیف فروش</summary>
    public OpportunityStage Stage { get; set; } = OpportunityStage.New;

    /// <summary>احتمال موفقیت (۰ تا ۱۰۰)</summary>
    public int Probability { get; set; } = 20;

    /// <summary>منبع آگاهی (معرفی، سایت، نمایشگاه و…)</summary>
    public string? Source { get; set; }

    /// <summary>تاریخ تخمینی بستن معامله</summary>
    public DateTime? ExpectedCloseAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>کارمند مسئول</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>زمان بسته‌شدن (برنده یا باخت)</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>عنوان فارسی مرحله — ذخیره نمی‌شود</summary>
    [NotMapped]
    public string StageTitle => Stage switch
    {
        OpportunityStage.New => "فرصت تازه",
        OpportunityStage.Contacted => "تماس برقرار شد",
        OpportunityStage.Qualified => "نیازسنجی",
        OpportunityStage.Proposal => "پیشنهاد قیمت",
        OpportunityStage.Negotiation => "مذاکره",
        OpportunityStage.Won => "برنده",
        _ => "از دست رفته"
    };

    /// <summary>رنگ نشان مرحله</summary>
    [NotMapped]
    public string StageBadge => Stage switch
    {
        OpportunityStage.New => "bg-secondary",
        OpportunityStage.Contacted => "bg-info text-dark",
        OpportunityStage.Qualified => "bg-primary",
        OpportunityStage.Proposal => "bg-warning text-dark",
        OpportunityStage.Negotiation => "bg-dark",
        OpportunityStage.Won => "bg-success",
        _ => "bg-danger"
    };

    /// <summary>آیا معامله بسته شده است؟</summary>
    [NotMapped]
    public bool IsClosed => Stage is OpportunityStage.Won or OpportunityStage.Lost;

    /// <summary>مبلغ وزن‌دار = مبلغ × احتمال</summary>
    [NotMapped]
    public decimal WeightedAmount => Math.Round(Amount * Probability / 100m, 0);

    /// <summary>آیا تاریخ تخمینی گذشته و معامله باز است؟</summary>
    [NotMapped]
    public bool IsLate
        => !IsClosed
           && ExpectedCloseAt.HasValue
           && ExpectedCloseAt.Value.Date < DateTime.Today;
}
