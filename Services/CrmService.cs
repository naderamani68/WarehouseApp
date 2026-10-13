using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>یک ردیف از قیف فروش برای نمایش در نمودار</summary>
public record CrmStageStat(OpportunityStage Stage, string Title, int Count, decimal Amount);

/// <summary>یک ردیف خلاصه از بهترین طرف حساب‌ها</summary>
public record CrmTopParty(string Name, int Orders, decimal Total);

/// <summary>اعداد داشبورد CRM</summary>
public class CrmStats
{
    public int ActiveParties { get; set; }
    public int CustomerCount { get; set; }
    public int SupplierCount { get; set; }

    public int OpenInteractions { get; set; }
    public int OverdueInteractions { get; set; }
    public int DueTodayInteractions { get; set; }

    public int OpenOpportunities { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WeightedPipeline { get; set; }
    public int WonOpportunities { get; set; }
    public decimal WonValue { get; set; }

    public int DraftOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public decimal ConfirmedSalesValue { get; set; }

    public List<CrmStageStat> Stages { get; set; } = new();
    public List<CrmInteraction> DueList { get; set; } = new();
    public List<CrmInteraction> RecentList { get; set; } = new();
    public List<CrmOpportunity> OpenOpportunityList { get; set; } = new();
    public List<CrmTopParty> TopParties { get; set; } = new();
}

/// <summary>
/// منطق مشترک CRM: شماره‌گذاری سفارش، تأیید سفارش (کسر از انبار) و اعداد داشبورد.
/// صفحه‌ها این منطق را دوباره نمی‌نویسند تا رفتار یکسان بماند.
/// </summary>
public static class CrmService
{
    // ================= شمارهٔ سفارش =================

    /// <summary>
    /// شمارهٔ بعدی سفارش را می‌سازد؛ قالب: SO-سال شمسی-چهار رقم
    /// </summary>
    public static async Task<string> NextOrderNumberAsync(ApplicationDbContext db)
    {
        var prefix = $"SO-{PersianDate.CurrentYear}-";

        var last = await db.SalesOrders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
        {
            next = n + 1;
        }

        return prefix + next.ToString("0000", CultureInfo.InvariantCulture);
    }

    // ================= تأیید سفارش =================

    /// <summary>
    /// سفارش پیش‌نویس را تأیید می‌کند: برای هر قلم یک حرکت «خروج» ثبت، از موجودی
    /// محصول کم، و رویداد در تاریخچهٔ عملیات نوشته می‌شود.
    /// اگر موجودی کافی نباشد، هیچ تغییری اعمال نمی‌شود.
    /// </summary>
    public static async Task<(bool Ok, string Message)> ConfirmOrderAsync(
        ApplicationDbContext db, int orderId, string? username)
    {
        var order = await db.SalesOrders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Party)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return (false, "سفارش پیدا نشد");
        }

        if (order.Status != OrderStatus.Draft)
        {
            return (false, "فقط سفارش پیش‌نویس قابل تأیید است");
        }

        if (order.Items.Count == 0)
        {
            return (false, "سفارش هیچ قلمی ندارد");
        }

        if (order.WarehouseId is null)
        {
            return (false, "برای تأیید سفارش، انبار خروج را انتخاب کنید");
        }

        // ===== بررسی موجودی پیش از هر تغییر =====
        var warehouseStock = await db.StockMovements
            .Where(m => m.WarehouseId == order.WarehouseId.Value)
            .GroupBy(m => m.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Type == MovementType.In ? x.Quantity : -x.Quantity)
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        var shortages = order.Items
            .Where(i => i.Product is not null && i.Quantity > i.Product.Quantity)
            .Select(i => $"{i.Product!.Name ?? "کالای بدون نام"} (موجودی: {i.Product.Quantity})")
            .ToList();

        if (shortages.Count > 0)
        {
            return (false, "موجودی کافی نیست: " + string.Join("، ", shortages));
        }

        // ===== کسر از انبار =====
        var warehouseShortages = order.Items
            .Where(i => i.Product is not null &&
                i.Quantity > (warehouseStock.TryGetValue(i.ProductId, out var inWarehouse) ? inWarehouse : 0))
            .Select(i => $"{i.Product!.Name ?? "کالای بدون نام"} (در این انبار: {(warehouseStock.TryGetValue(i.ProductId, out var inWarehouse) ? inWarehouse : 0)})")
            .ToList();

        if (warehouseShortages.Count > 0)
        {
            return (false, "در انبار انتخاب‌شده موجودی کافی نیست: " + string.Join("، ", warehouseShortages) + " — انبار دیگری را انتخاب کن");
        }

        foreach (var item in order.Items)
        {
            var product = await db.Products.FirstAsync(p => p.Id == item.ProductId);
            product.Quantity -= item.Quantity;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                WarehouseId = order.WarehouseId.Value,
                Type = MovementType.Out,
                Quantity = item.Quantity,
                MovementDate = DateTime.Now,
                Note = $"سفارش فروش {order.OrderNumber}"
            });
        }

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.Now;

        db.AuditLogs.Add(new AuditLog
        {
            Action = "تأیید",
            Entity = "سفارش فروش",
            EntityId = order.Id,
            Details = $"سفارش «{order.OrderNumber}» برای «{order.Party?.Name}» تأیید شد و {order.TotalQuantity} عدد از انبار کسر شد",
            Username = username
        });

        await db.SaveChangesAsync();

        return (true, $"سفارش «{order.OrderNumber}» تأیید شد و از انبار کسر شد");
    }

    // ================= داشبورد =================

    /// <summary>اعداد و فهرست‌های داشبورد CRM</summary>
    public static async Task<CrmStats> GetStatsAsync(ApplicationDbContext db)
    {
        var today = DateTime.Today;
        var stats = new CrmStats
        {
            ActiveParties = await db.Parties.CountAsync(p => p.IsActive),
            CustomerCount = await db.Parties.CountAsync(p => p.IsActive
                && (p.Type == PartyType.Customer || p.Type == PartyType.Both)),
            SupplierCount = await db.Parties.CountAsync(p => p.IsActive
                && (p.Type == PartyType.Supplier || p.Type == PartyType.Both))
        };

        // ===== پیگیری‌ها =====
        var openInteractions = db.CrmInteractions.Where(i => i.Status == InteractionStatus.Open);

        stats.OpenInteractions = await openInteractions.CountAsync();
        stats.OverdueInteractions = await openInteractions
            .CountAsync(i => i.FollowUpAt != null && i.FollowUpAt.Value.Date < today);
        stats.DueTodayInteractions = await openInteractions
            .CountAsync(i => i.FollowUpAt != null && i.FollowUpAt.Value.Date == today);

        stats.DueList = await openInteractions
            .Include(i => i.Party)
            .Where(i => i.FollowUpAt != null)
            .OrderBy(i => i.FollowUpAt)
            .Take(8)
            .ToListAsync();

        stats.RecentList = await db.CrmInteractions
            .Include(i => i.Party)
            .OrderByDescending(i => i.At)
            .Take(8)
            .ToListAsync();

        // ===== فرصت‌ها =====
        var openOpportunities = db.CrmOpportunities.Where(o => o.Stage != OpportunityStage.Won
                                                             && o.Stage != OpportunityStage.Lost);

        stats.OpenOpportunities = await openOpportunities.CountAsync();
        stats.PipelineValue = await openOpportunities.SumAsync(o => (decimal?)o.Amount) ?? 0m;
        stats.WeightedPipeline = await openOpportunities
            .SumAsync(o => (decimal?)(o.Amount * o.Probability / 100m)) ?? 0m;

        stats.WonOpportunities = await db.CrmOpportunities.CountAsync(o => o.Stage == OpportunityStage.Won);
        stats.WonValue = await db.CrmOpportunities
            .Where(o => o.Stage == OpportunityStage.Won)
            .SumAsync(o => (decimal?)o.Amount) ?? 0m;

        stats.OpenOpportunityList = await openOpportunities
            .Include(o => o.Party)
            .OrderByDescending(o => o.Amount)
            .Take(8)
            .ToListAsync();

        // ===== قیف فروش =====
        var stageGroups = await db.CrmOpportunities
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        foreach (var stage in new[]
                 {
                     OpportunityStage.New, OpportunityStage.Contacted, OpportunityStage.Qualified,
                     OpportunityStage.Proposal, OpportunityStage.Negotiation,
                     OpportunityStage.Won, OpportunityStage.Lost
                 })
        {
            var row = stageGroups.FirstOrDefault(g => g.Stage == stage);
            var sample = new CrmOpportunity { Stage = stage };
            stats.Stages.Add(new CrmStageStat(stage, sample.StageTitle, row?.Count ?? 0, row?.Amount ?? 0m));
        }

        // ===== سفارش‌ها =====
        stats.DraftOrders = await db.SalesOrders.CountAsync(o => o.Status == OrderStatus.Draft);
        stats.ConfirmedOrders = await db.SalesOrders.CountAsync(o => o.Status == OrderStatus.Confirmed);

        stats.ConfirmedSalesValue = await db.SalesOrders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .SelectMany(o => o.Items)
            .SumAsync(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.DiscountPercent / 100m))) ?? 0m;

        // ===== بهترین طرف حساب‌ها =====
        var top = await db.SalesOrders
            .Where(o => o.Status == OrderStatus.Confirmed && o.Party != null)
            .SelectMany(o => o.Items.Select(i => new
            {
                Name = o.Party!.Name,
                Orders = o.Id,
                Line = i.Quantity * i.UnitPrice * (1 - i.DiscountPercent / 100m)
            }))
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Orders = g.Select(x => x.Orders).Distinct().Count(), Total = g.Sum(x => x.Line) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync();

        stats.TopParties = top.Select(t => new CrmTopParty(t.Name, t.Orders, t.Total)).ToList();

        return stats;
    }
}
