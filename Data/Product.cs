using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Data;

/// <summary>واحد شمارش کالا</summary>
public enum ProductUnit
{
    [Display(Name = "عدد")] Number = 1,
    [Display(Name = "کارتن")] Carton = 2,
    [Display(Name = "کیلوگرم")] Kilogram = 3,
    [Display(Name = "لیتر")] Liter = 4,
    [Display(Name = "بسته")] Pack = 5,
    [Display(Name = "جعبه")] Box = 6,
    [Display(Name = "متر")] Meter = 7,
    [Display(Name = "خدمت")] Service = 8
}

/// <summary>
/// شناسنامهٔ کالا.
/// قیمت فروش و بهای خرید اینجا نگه‌داری می‌شود تا در سفارش‌ها خودکار پر شوند
/// و بتوان سود واقعی هر کالا را حساب کرد.
/// </summary>
public class Product
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>کد کالا — برای جستجوی سریع و فاکتور</summary>
    [MaxLength(50)]
    public string? Code { get; set; }

    /// <summary>بارکد کالا (اختیاری — برای آینده و دستگاه بارکدخوان)</summary>
    [MaxLength(50)]
    public string? Barcode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>موجودی کل — مجموع همهٔ انبارها</summary>
    public int Quantity { get; set; }

    public ProductUnit Unit { get; set; } = ProductUnit.Number;

    /// <summary>قیمت فروش هر واحد (ریال)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    /// <summary>بهای خرید هر واحد (ریال) — مبنای محاسبهٔ سود</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    /// <summary>حد سفارش — اگر موجودی به این عدد رسید، هشدار داده می‌شود</summary>
    public int ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    // ===== ارتباط با دسته (اختیاری) =====
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    // ===== ویژگی‌های محاسباتی (در پایگاه داده ذخیره نمی‌شوند) =====

    [NotMapped]
    public string UnitTitle => Unit switch
    {
        ProductUnit.Carton => "کارتن",
        ProductUnit.Kilogram => "کیلوگرم",
        ProductUnit.Liter => "لیتر",
        ProductUnit.Pack => "بسته",
        ProductUnit.Box => "جعبه",
        ProductUnit.Meter => "متر",
        ProductUnit.Service => "خدمت",
        _ => "عدد"
    };

    /// <summary>ارزش موجودی این کالا بر پایهٔ بهای خرید</summary>
    [NotMapped]
    public decimal StockValue => Quantity * CostPrice;

    /// <summary>سود هر واحد</summary>
    [NotMapped]
    public decimal ProfitPerUnit => SalePrice - CostPrice;

    /// <summary>درصد سود نسبت به بهای خرید</summary>
    [NotMapped]
    public decimal ProfitPercent => CostPrice <= 0 ? 0m : Math.Round(ProfitPerUnit / CostPrice * 100m, 1);

    [NotMapped]
    public bool IsOutOfStock => Quantity <= 0;

    /// <summary>آیا موجودی به حد سفارش رسیده است؟</summary>
    [NotMapped]
    public bool IsLow => ReorderLevel > 0 && Quantity > 0 && Quantity <= ReorderLevel;

    /// <summary>وضعیت موجودی برای نمایش</summary>
    [NotMapped]
    public string StockStatus
    {
        get
        {
            if (Quantity <= 0)
            {
                return "ناموجود";
            }

            if (IsLow)
            {
                return "کم — به حد سفارش رسید";
            }

            return "سالم";
        }
    }

    [NotMapped]
    public string StockBadge
    {
        get
        {
            if (Quantity <= 0)
            {
                return "bg-danger";
            }

            if (IsLow)
            {
                return "bg-warning text-dark";
            }

            return "bg-success";
        }
    }
}
