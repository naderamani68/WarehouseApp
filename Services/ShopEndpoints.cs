using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// نقطه‌های ورود فروشگاه (فرم‌های معمولی POST).
///
/// چرا فرم معمولی و نه کد تعاملی؟ چون صفحه‌های فروشگاه استاتیک رندر می‌شوند؛
/// این‌طور هر بازدیدکنندهٔ ناشناس یک اتصال زندهٔ سروری اشغال نمی‌کند و سرور
/// با ترافیک عمومی از پا در نمی‌آید. محافظت در برابر جعل درخواست هم با
/// نشانهٔ ضدجعل داخل فرم‌ها انجام می‌شود.
/// </summary>
public static class ShopEndpoints
{
    public static void MapShopEndpoints(this WebApplication app)
    {
        // ===== افزودن به سبد =====
        app.MapPost("/shop/cart/add", async (
            HttpContext context,
            ApplicationDbContext db,
            [FromForm] int productId,
            [FromForm] int quantity,
            [FromForm] string? returnUrl) =>
        {
            var cart = ShopCart.Read(context);

            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product is not null)
            {
                var wanted = cart.TryGetValue(productId, out var current) ? current + quantity : quantity;
                wanted = Math.Clamp(wanted, 1, 999);

                // هرگز بیشتر از موجودی انبار اجازه نمی‌دهیم
                if (product.Quantity > 0)
                {
                    wanted = Math.Min(wanted, product.Quantity);
                }

                if (wanted > 0)
                {
                    cart[productId] = wanted;
                    ShopCart.Write(context, cart);
                }
            }

            return Results.Redirect(SafeReturn(returnUrl));
        });

        // ===== تغییر تعداد یا حذف یک قلم =====
        app.MapPost("/shop/cart/update", (
            HttpContext context,
            [FromForm] int productId,
            [FromForm] int quantity) =>
        {
            var cart = ShopCart.Read(context);

            if (quantity <= 0)
            {
                cart.Remove(productId);
            }
            else
            {
                cart[productId] = Math.Clamp(quantity, 1, 999);
            }

            ShopCart.Write(context, cart);

            return Results.Redirect("/shop/cart");
        });

        // ===== خالی کردن سبد =====
        app.MapPost("/shop/cart/clear", (HttpContext context) =>
        {
            ShopCart.Clear(context);
            return Results.Redirect("/shop/cart");
        });

        // ===== ثبت سفارش مهمان (بدون ثبت‌نام) =====
        app.MapPost("/shop/cart/checkout", async (
            HttpContext context,
            ApplicationDbContext db,
            [FromForm] string? name,
            [FromForm] string? phone,
            [FromForm] string? address,
            [FromForm] string? note) =>
        {
            var cart = ShopCart.Read(context);

            if (cart.Count == 0)
            {
                return Results.Redirect("/shop/cart");
            }

            var customerName = (name ?? string.Empty).Trim();
            var customerPhone = ShopCatalogService.NormalizePhone(phone);
            var customerAddress = (address ?? string.Empty).Trim();
            var customerNote = (note ?? string.Empty).Trim();

            if (customerName.Length < 3 || customerPhone.Length < 10)
            {
                return Results.Redirect("/shop/cart?error=info");
            }

            // قیمت هرگز از سمت مرورگر خوانده نمی‌شود؛ همیشه از پایگاه داده می‌آید
            var products = await db.Products
                .Where(p => cart.Keys.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            if (products.Count == 0)
            {
                return Results.Redirect("/shop/cart?error=empty");
            }

            var party = await db.Parties
                .FirstOrDefaultAsync(x => x.Mobile == customerPhone || x.Phone == customerPhone);

            if (party is null)
            {
                party = new Party
                {
                    Code = "WEB-" + Guid.NewGuid().ToString("N")[..8],
                    Name = customerName,
                    Type = PartyType.Customer,
                    Mobile = customerPhone,
                    Phone = customerPhone,
                    Address = customerAddress,
                    Notes = "ثبت‌شده از فروشگاه اینترنتی",
                    CreatedAt = DateTime.Now
                };

                db.Parties.Add(party);
                await db.SaveChangesAsync();
            }
            else if (customerAddress.Length > 0)
            {
                party.Address = customerAddress;
            }

            var warehouseId = await db.Warehouses
                .OrderBy(w => w.Id)
                .Select(w => (int?)w.Id)
                .FirstOrDefaultAsync();

            var order = new SalesOrder
            {
                OrderNumber = "TEMP",
                PartyId = party.Id,
                WarehouseId = warehouseId,
                OrderDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Status = OrderStatus.Draft,
                Source = OrderSource.Shop,
                Username = "shop",
                Notes = BuildNotes(customerAddress, customerNote)
            };

            foreach (var product in products)
            {
                var quantity = cart[product.Id];

                if (product.Quantity > 0)
                {
                    quantity = Math.Min(quantity, product.Quantity);
                }

                if (quantity <= 0)
                {
                    continue;
                }

                order.Items.Add(new SalesOrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.SalePrice,
                    DiscountPercent = 0
                });
            }

            if (order.Items.Count == 0)
            {
                return Results.Redirect("/shop/cart?error=stock");
            }

            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // شمارهٔ سفارش بعد از ذخیره ساخته می‌شود تا یکتا باشد
            order.OrderNumber = $"SO-{order.Id:00000}";
            await db.SaveChangesAsync();

            ShopCart.Clear(context);

            return Results.Redirect($"/shop/order/{order.OrderNumber}");
        });
    }

    /// <summary>فقط نشانی‌های داخلی فروشگاه برای بازگشت پذیرفته می‌شوند</summary>
    private static string SafeReturn(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/shop", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        return "/shop/cart";
    }

    private static string BuildNotes(string address, string note)
    {
        var text = "سفارش فروشگاه اینترنتی";

        if (address.Length > 0)
        {
            text += $"\nنشانی: {address}";
        }

        if (note.Length > 0)
        {
            text += $"\nیادداشت مشتری: {note}";
        }

        return text;
    }
}
