using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// کارهای موجودی انبار.
/// «انتقال» موجودی کل کالا را تغییر نمی‌دهد؛ فقط یک حرکت خروج از انبار مبدأ
/// و یک حرکت ورود به انبار مقصد ثبت می‌کند.
/// اگر مبدأ «بدون انبار» باشد (شناسهٔ منفی)، تنها حرکت ورود ثبت می‌شود؛
/// یعنی همان «ثبت موجودی دفتری در یک انبار».
/// </summary>
public static class StockService
{
    public static async Task<(bool Ok, string Message)> TransferAsync(
        ApplicationDbContext db, int productId, int fromWarehouseId, int toWarehouseId, int quantity)
    {
        if (quantity <= 0)
        {
            return (false, "مقدار انتقال باید بیشتر از صفر باشد");
        }

        if (toWarehouseId <= 0)
        {
            return (false, "انبار مقصد را انتخاب کن");
        }

        if (toWarehouseId == fromWarehouseId)
        {
            return (false, "انبار مبدأ و مقصد یکی است");
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return (false, "کالا پیدا نشد");
        }

        var target = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == toWarehouseId);

        if (target is null)
        {
            return (false, "انبار مقصد در سیستم نیست");
        }

        string sourceName;

        if (fromWarehouseId <= 0)
        {
            var unassigned = product.Quantity
                - await db.StockMovements.Where(m => m.ProductId == productId)
                    .SumAsync(m => m.Type == MovementType.In ? m.Quantity : -m.Quantity);

            if (quantity > unassigned)
            {
                return (false, $"فقط {unassigned} {product.UnitTitle} بدون انبار است");
            }

            sourceName = "بدون انبار";
        }
        else
        {
            var source = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == fromWarehouseId);

            if (source is null)
            {
                return (false, "انبار مبدأ در سیستم نیست");
            }

            sourceName = source.Name ?? "—";

            var available = await db.StockMovements
                .Where(m => m.ProductId == productId && m.WarehouseId == fromWarehouseId)
                .SumAsync(m => m.Type == MovementType.In ? m.Quantity : -m.Quantity);

            if (quantity > available)
            {
                return (false, $"در «{sourceName}» فقط {available} {product.UnitTitle} موجود است");
            }

            db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = fromWarehouseId,
                Type = MovementType.Out,
                Quantity = quantity,
                MovementDate = DateTime.Now,
                Note = $"انتقال به {target.Name}"
            });
        }

        db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            WarehouseId = toWarehouseId,
            Type = MovementType.In,
            Quantity = quantity,
            MovementDate = DateTime.Now,
            Note = $"انتقال از {sourceName}"
        });

        await db.SaveChangesAsync();

        return (true, $"انتقال {quantity} {product.UnitTitle} از «{sourceName}» به «{target.Name}» انجام شد");
    }
}