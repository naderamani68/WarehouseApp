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

        var query = db.Products.AsNoTracking().Where(p => p.IsActive && p.ShowInShop);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                (p.Name != null && p.Name.Contains(term)) ||
                (p.Code != null && p.Code.Contains(term)));
        }

        if (categoryId is > 0)
        {
            // کالاهای زیردسته‌ها هم نمایش داده می‌شوند
            var ids = await DescendantIdsAsync(db, categoryId.Value);
            query = query.Where(p => p.CategoryId != null && ids.Contains(p.CategoryId.Value));
        }

        // نکته: ترتیبدهی باید روی ستون‌های واقعی جدول انجام شود، نه روی ویژگی‌های
        // محاسباتی مدل سبک؛ وگرنه موتور پایگاه داده نمی‌تواند آن را ترجمه کند.
        var rows = await query
            .Include(p => p.Category)
            .OrderByDescending(p => p.Quantity > 0)
            .ThenBy(p => p.Name)
            .Take(take)
            .ToListAsync();

        var items = rows.Select(Map).ToList();

        await AttachGalleryAsync(db, items);

        return items;
    }

    /// <summary>یک کالا برای صفحهٔ جزئیات</summary>
    public async Task<ShopProductDto?> GetProductAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.IsActive && p.ShowInShop && p.Id == id);

        if (product is null)
        {
            return null;
        }

        var item = Map(product);

        await AttachGalleryAsync(db, new List<ShopProductDto> { item });

        return item;
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

        var rows = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ShowInShop && wanted.Contains(p.Id))
            .ToListAsync();

        var items = rows.Select(Map).ToList();

        await AttachGalleryAsync(db, items);

        return items;
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

    /// <summary>درخت دسته‌ها با شمارش کالا — برای نوار کناری فروشگاه</summary>
    public async Task<List<ShopCategoryNode>> GetCategoryTreeAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        var rows = await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                Name = c.Name ?? "بی‌نام",
                c.ParentId,
                Count = c.Products.Count(p => p.IsActive && p.ShowInShop)
            })
            .ToListAsync();

        // شمارش هر دسته به‌همراه همهٔ زیردسته‌هایش
        var totals = rows.ToDictionary(r => r.Id, r => r.Count);

        foreach (var row in rows.Where(r => r.ParentId is not null))
        {
            var parentId = row.ParentId!.Value;

            for (var step = 0; step < 20 && totals.ContainsKey(parentId); step++)
            {
                totals[parentId] += row.Count;
                parentId = rows.FirstOrDefault(r => r.Id == parentId)?.ParentId ?? 0;
            }
        }

        var nodes = rows.ToDictionary(
            r => r.Id,
            r => new ShopCategoryNode(r.Id, r.Name, r.ParentId, r.Count)
            {
                TotalCount = totals[r.Id]
            });

        var roots = new List<ShopCategoryNode>();

        foreach (var node in nodes.Values)
        {
            if (node.ParentId is not null && nodes.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        Sort(roots);
        return roots;

        static void Sort(List<ShopCategoryNode> list)
        {
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));

            foreach (var node in list)
            {
                Sort(node.Children);
            }
        }
    }

    /// <summary>شناسهٔ یک دسته و همهٔ زیردسته‌هایش</summary>
    private static async Task<List<int>> DescendantIdsAsync(ApplicationDbContext db, int rootId)
    {
        var pairs = await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync();

        var result = new List<int> { rootId };
        var frontier = new List<int> { rootId };

        for (var depth = 0; depth < 10 && frontier.Count > 0; depth++)
        {
            var parents = frontier;

            frontier = pairs
                .Where(p => p.ParentId is not null && parents.Contains(p.ParentId.Value))
                .Select(p => p.Id)
                .Where(id => !result.Contains(id))
                .Distinct()
                .ToList();

            result.AddRange(frontier);
        }

        return result;
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
            .Include(o => o.Channel)
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
            order.Party?.Name,
            order.Channel != null ? order.Channel.Name : null);
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

    /// <summary>پیوست گالری عکس به کالاها — با یک پرس‌وجو برای همه</summary>
    private static async Task AttachGalleryAsync(ApplicationDbContext db, List<ShopProductDto> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var ids = items.Select(i => i.Id).ToList();

        var images = await db.ProductImages
            .AsNoTracking()
            .Where(i => ids.Contains(i.ProductId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.ProductId, i.Path })
            .ToListAsync();

        var map = images
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Path).ToList());

        foreach (var item in items)
        {
            if (map.TryGetValue(item.Id, out var paths))
            {
                item.Gallery.AddRange(paths);
            }
        }
    }

    /// <summary>تبدیل کالا به مدل سبک فروشگاه — بعد از خواندن از پایگاه داده و در حافظه</summary>
    private static ShopProductDto Map(Product p)
    {
        return new ShopProductDto(
            p.Id,
            p.Name ?? "کالای بی‌نام",
            p.Code,
            p.Category?.Name,
            p.Unit,
            p.SalePrice,
            p.Quantity > 0,
            p.Quantity,
            p.ImagePath,
            p.Description);
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
    /// <summary>عکس‌های گالری کالا</summary>
    public List<string> Gallery { get; } = new();

    /// <summary>عکس اصلی: عکس تکی و اگر نبود، نخستین عکس گالری</summary>
    public string? MainImage => string.IsNullOrWhiteSpace(ImagePath) ? Gallery.FirstOrDefault() : ImagePath;

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

/// <summary>گرهٔ درخت دسته در فروشگاه — دستهٔ مادر با زیردسته‌هایش</summary>
public record ShopCategoryNode(int Id, string Name, int? ParentId, int ProductCount)
{
    /// <summary>شمارش کالاهای این دسته به‌همراه زیردسته‌ها</summary>
    public int TotalCount { get; init; }

    public List<ShopCategoryNode> Children { get; } = new();
}

/// <summary>وضعیت سفارش برای پیگیری مشتری</summary>
public record ShopOrderDto(
    string Number,
    DateTime OrderDate,
    string StatusTitle,
    string StatusBadge,
    int ItemCount,
    decimal Total,
    string? CustomerName,
    string? ChannelName);