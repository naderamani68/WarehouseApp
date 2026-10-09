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

        // نکته: ویژگی‌های محاسباتی مثل «عنوان واحد» در پایگاه داده ترجمه نمی‌شوند،
        // پس اول فقط ستون‌های واقعی را می‌خوانیم و بعد در حافظه تبدیل می‌کنیم.
        var rows = await query
            .OrderByDescending(p => p.Quantity > 0)
            .ThenBy(p => p.Name)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.Unit,
                p.SalePrice,
                p.Quantity
            })
            .ToListAsync();

        return rows
            .Select(r => new ShopProductDto(
                r.Id,
                string.IsNullOrWhiteSpace(r.Name) ? "کالای بی‌نام" : r.Name!,
                r.Code,
                r.CategoryName,
                r.Unit,
                r.SalePrice,
                r.Quantity > 0))
            .ToList();
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
}

/// <summary>مدل سبک کالا برای نمایش در فروشگاه — هیچ فیلد داخلی انبار در آن نیست</summary>
public record ShopProductDto(
    int Id,
    string Name,
    string? Code,
    string? CategoryName,
    ProductUnit Unit,
    decimal Price,
    bool InStock)
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
