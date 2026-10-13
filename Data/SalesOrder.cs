using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>
/// وضعیت سفارش فروش
/// </summary>
public enum OrderStatus
{
    /// <summary>پیش‌نویس — قابل ویرایش، بی‌اثر روی انبار</summary>
    Draft = 1,

    /// <summary>تأییدشده — از انبار کسر شده است</summary>
    Confirmed = 2,

    /// <summary>لغو‌شده</summary>
    Canceled = 3
}

/// <summary>
/// منبع سفارش — از کدام دروازه ثبت شده است
/// </summary>
public enum OrderSource
{
    /// <summary>پنل انبار (دستی)</summary>
    Panel = 1,

    /// <summary>فروشگاه اینترنتی</summary>
    Shop = 2,

    /// <summary>پنل فروشندگان</summary>
    SalesRep = 3
}

/// <summary>
/// سفارش فروش — سربرگ سفارش.
/// تا زمانی که «پیش‌نویس» است روی انبار اثری ندارد؛ با تأیید، برای هر قلم یک
/// حرکت «خروج» ثبت و از موجودی محصول کم می‌شود.
/// </summary>
public class SalesOrder
{
        public decimal VatPercent { get; set; }
    public int Id { get; set; }

    /// <summary>شمارهٔ سفارش (یکتا) — مثل SO-1405-0001</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>منبع ثبت سفارش (پنل، فروشگاه یا فروشنده)</summary>
    public OrderSource Source { get; set; } = OrderSource.Panel;

    /// <summary>کانال فروش — کالا کجا فروخته شد (حضوری، دیوار، اینستاگرام…)</summary>
    public int? ChannelId { get; set; }

    public SalesChannel? Channel { get; set; }

    public int PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>انباری که کالا از آن خارج می‌شود</summary>
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public string? Notes { get; set; }

    /// <summary>کارمند ثبت‌کننده</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>زمان تأیید</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>دلیل لغو</summary>
    public string? CancelReason { get; set; }

    public List<SalesOrderItem> Items { get; set; } = new();

    /// <summary>جمع کل سفارش — ذخیره نمی‌شود</summary>
    [NotMapped]
    public decimal Total => Items.Sum(i => i.LineTotal);

    /// <summary>تعداد کل اقلام</summary>
    [NotMapped]
    public int TotalQuantity => Items.Sum(i => i.Quantity);

    /// <summary>عنوان فارسی وضعیت</summary>
    [NotMapped]
    public string StatusTitle => Status switch
    {
        OrderStatus.Draft => "پیش‌نویس",
        OrderStatus.Confirmed => "تأییدشده",
        _ => "لغو‌شده"
    };

    /// <summary>رنگ نشان وضعیت</summary>
    [NotMapped]
    public string StatusBadge => Status switch
    {
        OrderStatus.Draft => "bg-warning text-dark",
        OrderStatus.Confirmed => "bg-success",
        _ => "bg-danger"
    };
}

/// <summary>
/// یک قلم از سفارش فروش
/// </summary>
public class SalesOrderItem
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>قیمت واحد</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>درصد تخفیف این قلم (۰ تا ۱۰۰)</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// بهای تمام‌شدهٔ هر واحد در لحظهٔ فروش.
    /// عکس گرفته می‌شود تا تغییر قیمت خرید کالا، سود سفارش‌های قبلی را عوض نکند.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>بهای تمام‌شدهٔ کل این قلم — ذخیره نمی‌شود</summary>
    [NotMapped]
    public decimal CostTotal => Math.Round(Quantity * CostPrice, 0);

    /// <summary>جمع این قلم پس از تخفیف — ذخیره نمی‌شود</summary>
    [NotMapped]
    public decimal LineTotal
        => Math.Round(Quantity * UnitPrice * (1 - DiscountPercent / 100m), 0);
}
