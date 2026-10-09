using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// خواندن داده برای فروشگاه عمومی.
///
/// قاعدهٔ امنیتی: فروشگاه هرگز مستقیم به جدول‌ها دسترسی ندارد و هیچ‌وقت خودکار
/// «جاندار» جدول‌ها را بیرون نمی‌دهد؛ فقط مدل سبک و بی‌خطر برمی‌گرداند.
/// این‌طور اطلاعات کارمندان، حقوق و حساب‌ها هرگز به بیرون درز نمی‌کند.
/// </summary>
public class ShopCatalogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ShopCatalogService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>فهرست کالاهای فعال برای نمایش در فروشگاه</summary>
    public async Task<List<ShopProductDto>> GetProductsAsync(string? search = null, int? categoryId = null, int take = 60)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                (p.Name != null && p.Name.Contains(term)) ||
                (p.Code != null && p.Code.Contains(term)));
        }

        if (categoryId is > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        return await Project(query)
            .OrderByDescending(p => p.InStock)
            .ThenBy(p => p.Name)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>یک کالا برای صفحهٔ جزئیات</summary>
    public async Task<ShopProductDto?> GetProductAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await Project(db.Products.AsNoTracking().Where(p => p.IsActive && p.Id == id))
            .FirstOrDefaultAsync();
    }

    /// <summary>چند کالا با شناسه — برای سبد خرید</summary>
    public async Task<List<ShopProductDto>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        var wanted = ids?.Distinct().Take(60).ToList() ?? new List<int>();

        if (wanted.Count == 0)
        {
            return new List<ShopProductDto>();
        }

        await using var db = await _factory.CreateDbContextAsync();

        return await Project(db.Products.AsNoTracking().Where(p => p.IsActive && wanted.Contains(p.Id)))
            .ToListAsync();
    }

    /// <summary>فهرست دسته‌هایی که حداقل یک کالای فعال دارند</summary>
    public async Task<List<ShopCategoryDto>> GetCategoriesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Categories
            .AsNoTracking()
            .Where(c => c.Products.Any(p => p.IsActive))
            .OrderBy(c => c.Name)
            .Select(c => new ShopCategoryDto(c.Id, c.Name ?? "بی‌نام"))
            .ToListAsync();
    }

    /// <summary>
    /// وضعیت یک سفارش برای مشتری — فقط اگر شمارهٔ تلفن با ثبت‌کنندهٔ سفارش بخواند.
    /// این‌طور کسی نمی‌تواند با حدس زدن شمارهٔ سفارش، سفارش دیگران را ببیند.
    /// </summary>
    public async Task<ShopOrderDto?> GetOrderAsync(string number, string? phone)
    {
        var wanted = NormalizePhone(phone);

        if (string.IsNullOrWhiteSpace(number) || wanted.Length < 10)
        {
            return null;
        }

        await using var db = await _factory.CreateDbContextAsync();

        var order = await db.SalesOrders
            .AsNoTracking()
            .Include(o => o.Party)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == number.Trim());

        if (order is null)
        {
            return null;
        }

        var owner = NormalizePhone(order.Party?.Mobile);

        if (owner.Length < 10)
        {
            owner = NormalizePhone(order.Party?.Phone);
        }

        if (owner != wanted)
        {
            return null;
        }

        return new ShopOrderDto(
            order.OrderNumber,
            order.OrderDate,
            order.StatusTitle,
            order.StatusBadge,
            order.TotalQuantity,
            order.Total,
            order.Party?.Name);
    }

    /// <summary>یکسان‌سازی شمارهٔ تلفن: رقم فارسی به لاتین، حذف نویسه‌های اضافه، ده رقم آخر</summary>
    public static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var ch in value)
        {
            if (ch is >= '0' and <= '9')
            {
                builder.Append(ch);
            }
            else if (ch is >= '۰' and <= '۹')
            {
                builder.Append((char)('0' + (ch - '۰')));
            }
        }

        var digits = builder.ToString();

        return digits.Length > 10 ? digits[^10..] : digits;
    }

    /// <summary>تبدیل پرس‌وجو به مدل سبک فروشگاه</summary>
    private static IQueryable<ShopProductDto> Project(IQueryable<Product> query)
    {
        return query.Select(p => new ShopProductDto(
            p.Id,
            p.Name ?? "کالای بی‌نام",
            p.Code,
            p.Category != null ? p.Category.Name : null,
            p.Unit,
            p.SalePrice,
            p.Quantity > 0,
            p.Quantity,
            p.ImagePath,
            p.Description));
    }
}

/// <summary>مدل سبک کالا برای نمایش در فروشگاه — هیچ فیلد داخلی انبار در آن نیست</summary>
public record ShopProductDto(
    int Id,
    string Name,
    string? Code,
    string? CategoryName,
    ProductUnit Unit,
    decimal Price,
    bool InStock,
    int Available,
    string? ImagePath,
    string? Description)
{
    /// <summary>عنوان فارسی واحد کالا</summary>
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
}

/// <summary>مدل سبک دستهٔ کالا برای فروشگاه</summary>
public record ShopCategoryDto(int Id, string Name);

/// <summary>وضعیت سفارش برای پیگیری مشتری</summary>
public record ShopOrderDto(
    string Number,
    DateTime OrderDate,
    string StatusTitle,
    string StatusBadge,
    int ItemCount,
    decimal Total,
    string? CustomerName);