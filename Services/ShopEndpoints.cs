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
            IConfiguration configuration,
            SmsService sms,
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
            var customerPhone = Mobile.Normalize(phone);
            var customerAddress = (address ?? string.Empty).Trim();
            var customerNote = (note ?? string.Empty).Trim();

            if (customerName.Length < 3 || !Mobile.IsValid(customerPhone))
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

            // انبار پس از مشخص‌شدن اقلام انتخاب می‌شود (پایین‌تر) تا از انباری که موجودی دارد کسر شود.

            var shopChannelId = await db.SalesChannels
                .Where(c => c.Name == "فروشگاه اینترنتی")
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            var vatNow = await SettingsService.VatEnabledAsync(db) ? await SettingsService.VatPercentAsync(db) : 0m;

            var order = new SalesOrder
            {
                OrderNumber = "TEMP",
                PartyId = party.Id,

                OrderDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Status = OrderStatus.Draft,
                Source = OrderSource.Shop,
                ChannelId = shopChannelId,
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

                order.VatPercent = vatNow;
                order.Items.Add(new SalesOrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.SalePrice,
                    DiscountPercent = 0,
                    CostPrice = product.CostPrice
                });
            }

            if (order.Items.Count == 0)
            {
                return Results.Redirect("/shop/cart?error=stock");
            }

            // ===== انتخاب انبار خروج: انباری که برای این اقلام موجودی دارد =====
            var orderedProductIds = order.Items.Select(i => i.ProductId).ToList();
            var stockByWarehouse = await db.StockMovements
                .Where(m => orderedProductIds.Contains(m.ProductId))
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new
                {
                    g.Key.WarehouseId,
                    Available = g.Sum(x => x.Type == WarehouseApp.Data.MovementType.In ? x.Quantity : -x.Quantity)
                })
                .ToListAsync();

            var bestWarehouse = stockByWarehouse
                .GroupBy(x => x.WarehouseId)
                .Select(g => new { WarehouseId = g.Key, Score = g.Sum(x => x.Available) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            order.WarehouseId = bestWarehouse is not null && bestWarehouse.Score > 0
                ? bestWarehouse.WarehouseId
                : await db.Warehouses.OrderBy(w => w.Id).Select(w => (int?)w.Id).FirstOrDefaultAsync();

            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // شمارهٔ سفارش بعد از ذخیره ساخته می‌شود تا یکتا باشد
            order.OrderNumber = $"SO-{order.Id:00000}";
            await db.SaveChangesAsync();

            // ===== پیامک تأیید برای مشتری =====
            var baseUrl = (configuration["Shop:BaseUrl"] ?? "http://localhost:5023").TrimEnd('/');
            var total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

            await sms.SendAsync(
                customerPhone,
                $"سفارش شما با شماره {order.OrderNumber} ثبت شد. مبلغ {total:N0} ریال. پیگیری: {baseUrl}/shop/order/{order.OrderNumber}");

            ShopCart.Clear(context);

            return Results.Redirect($"/shop/order/{order.OrderNumber}");
        });

        // ===== راهنمای ربات‌های جستجو =====
        app.MapGet("/robots.txt", (IConfiguration configuration) =>
        {
            var baseUrl = (configuration["Shop:BaseUrl"] ?? "http://localhost:5023").TrimEnd('/');

            var text = "User-agent: *\n" +
                       "Allow: /shop\n" +
                       "Disallow: /account/\n" +
                       "Disallow: /admin/\n" +
                       "Disallow: /crm/\n" +
                       "Disallow: /finance/\n" +
                       "Disallow: /warehouse/\n" +
                       "Disallow: /sales/\n" +
                       "Disallow: /settings\n" +
                       $"Sitemap: {baseUrl}/sitemap.xml\n";

            return Results.Text(text, "text/plain");
        });

        // ===== نقشهٔ سایت برای موتور جستجو =====
        app.MapGet("/sitemap.xml", async (IConfiguration configuration, ApplicationDbContext db) =>
        {
            var baseUrl = (configuration["Shop:BaseUrl"] ?? "http://localhost:5023").TrimEnd('/');

            var ids = await db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.ShowInShop)
                .OrderBy(p => p.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            builder.AppendLine($"  <url><loc>{baseUrl}/shop</loc></url>");

            foreach (var id in ids)
            {
                builder.AppendLine($"  <url><loc>{baseUrl}/shop/product/{id}</loc></url>");
            }

            builder.AppendLine("</urlset>");

            return Results.Text(builder.ToString(), "application/xml", System.Text.Encoding.UTF8);
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
