using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>جمع هزینه در یک دسته</summary>
public record FinanceCategoryStat(ExpenseCategory Category, string Title, string Icon, string Badge, decimal Amount, int Count);

/// <summary>جمع هزینه در یک ماه شمسی</summary>
public record FinanceMonthStat(int Year, int Month, string Title, decimal Amount);

/// <summary>هزینهٔ یک کالا</summary>
public record FinanceProductCost(int ProductId, string ProductName, decimal Quantity, decimal Amount);

/// <summary>تصویر کامل وضعیت مالی</summary>
public record FinanceStats(
    int PendingIncomingCount, decimal PendingIncomingAmount,
    int PendingOutgoingCount, decimal PendingOutgoingAmount,
    int OverdueChequeCount, decimal OverdueChequeAmount,
    int ReturnedChequeCount,
    List<Cheque> DueSoonList,
    List<Cheque> RecentCheques,
    int OpenPayableCount, decimal OpenPayableAmount,
    int OpenReceivableCount, decimal OpenReceivableAmount,
    int OverdueDebtCount,
    List<Debt> OpenDebtList,
    decimal ExpenseThisMonth, decimal ExpenseThisYear, decimal ExpenseTotal,
    int ExpenseCount,
    List<FinanceCategoryStat> ByCategory,
    List<FinanceMonthStat> Monthly,
    List<FinanceProductCost> ProductCosts,
    decimal SaleValue, int ConfirmedOrders,
    decimal EstimatedBalance);

/// <summary>
/// منطق مشترک بخش مالی: چک و سفته، بدهی و طلب، هزینه‌ها و محاسبهٔ وضعیت مالی.
/// محاسبه‌ها در حافظه انجام می‌شود؛ حجم دادهٔ یک کسب‌وکار کوچک کم است و این کار
/// کد را ساده‌تر و قابل‌اعتمادتر می‌کند.
/// </summary>
public static class FinanceService
{
    private static readonly PersianCalendar Pc = new();

    /// <summary>نام ماه‌های شمسی</summary>
    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    public static async Task<FinanceStats> GetStatsAsync(ApplicationDbContext db)
    {
        var today = DateTime.Today;

        var cheques = await db.Cheques.Include(c => c.Party).ToListAsync();
        var debts = await db.Debts.Include(d => d.Party).ToListAsync();
        var expenses = await db.Expenses.Include(e => e.Product).ToListAsync();
        var orders = await db.SalesOrders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Confirmed)
            .ToListAsync();

        // ===== چک و سفته =====
        var pending = cheques.Where(c => c.Status == ChequeStatus.Pending).ToList();

        var pendingIncoming = pending.Where(c => c.Direction == ChequeDirection.Incoming).ToList();
        var pendingOutgoing = pending.Where(c => c.Direction == ChequeDirection.Outgoing).ToList();

        var overdueCheques = pending.Where(c => c.DueDate.Date < today).ToList();

        var dueSoon = pending
            .Where(c => c.DueDate.Date <= today.AddDays(30))
            .OrderBy(c => c.DueDate)
            .Take(20)
            .ToList();

        var recentCheques = cheques
            .OrderByDescending(c => c.CreatedAt)
            .Take(8)
            .ToList();

        // ===== بدهی و طلب =====
        var openDebts = debts.Where(d => !d.IsSettled).ToList();

        var payable = openDebts.Where(d => d.Type == DebtType.Payable).ToList();
        var receivable = openDebts.Where(d => d.Type == DebtType.Receivable).ToList();

        var overdueDebtCount = openDebts.Count(d => d.IsOverdue);

        var openDebtList = openDebts
            .OrderBy(d => d.DueDate ?? DateTime.MaxValue)
            .ThenBy(d => d.IssueDate)
            .Take(20)
            .ToList();

        // ===== هزینه‌ها =====
        var expenseTotal = expenses.Sum(e => e.Amount);

        var persianYear = Pc.GetYear(today);
        var persianMonth = Pc.GetMonth(today);

        var expenseThisMonth = expenses
            .Where(e => Pc.GetYear(e.At) == persianYear && Pc.GetMonth(e.At) == persianMonth)
            .Sum(e => e.Amount);

        var expenseThisYear = expenses
            .Where(e => Pc.GetYear(e.At) == persianYear)
            .Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g =>
            {
                var sample = new Expense { Category = g.Key };
                return new FinanceCategoryStat(g.Key, sample.CategoryTitle, sample.CategoryIcon, sample.CategoryBadge, g.Sum(x => x.Amount), g.Count());
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // شش ماه شمسی گذشته، از قدیم به جدید
        var monthly = new List<FinanceMonthStat>();
        var y = persianYear;
        var m = persianMonth;

        for (var k = 0; k < 6; k++)
        {
            var amount = expenses
                .Where(e => Pc.GetYear(e.At) == y && Pc.GetMonth(e.At) == m)
                .Sum(e => e.Amount);

            monthly.Insert(0, new FinanceMonthStat(y, m, MonthNames[m - 1], amount));

            m--;
            if (m == 0)
            {
                m = 12;
                y--;
            }
        }

        var productCosts = expenses
            .Where(e => e.ProductId.HasValue)
            .GroupBy(e => e.ProductId!.Value)
            .Select(g => new FinanceProductCost(
                g.Key,
                g.First().Product?.Name ?? "کالای حذف‌شده",
                g.Sum(x => x.Quantity ?? 0m),
                g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ===== فروش و ماندهٔ تقریبی =====
        var saleValue = orders.Sum(o => o.Items.Sum(i => i.LineTotal));

        var estimatedBalance = saleValue - expenseTotal
                               + receivable.Sum(d => d.Remaining)
                               - payable.Sum(d => d.Remaining);

        return new FinanceStats(
            pendingIncoming.Count, pendingIncoming.Sum(c => c.Amount),
            pendingOutgoing.Count, pendingOutgoing.Sum(c => c.Amount),
            overdueCheques.Count, overdueCheques.Sum(c => c.Amount),
            cheques.Count(c => c.Status == ChequeStatus.Returned),
            dueSoon,
            recentCheques,
            payable.Count, payable.Sum(d => d.Remaining),
            receivable.Count, receivable.Sum(d => d.Remaining),
            overdueDebtCount,
            openDebtList,
            expenseThisMonth, expenseThisYear, expenseTotal,
            expenses.Count,
            byCategory,
            monthly,
            productCosts,
            saleValue, orders.Count,
            estimatedBalance);
    }

    /// <summary>
    /// ثبت پرداخت (کامل یا جزئی) روی یک بدهی یا طلب.
    /// اگر مانده صفر شود، وضعیت خودکار «تسویه‌شده» می‌شود.
    /// </summary>
    public static async Task<(bool Ok, string Message)> RegisterPaymentAsync(
        ApplicationDbContext db, int debtId, decimal amount, string? username)
    {
        if (amount <= 0)
        {
            return (false, "مبلغ پرداخت باید بزرگ‌تر از صفر باشد");
        }

        var debt = await db.Debts.FirstOrDefaultAsync(d => d.Id == debtId);
        if (debt is null)
        {
            return (false, "مورد مالی پیدا نشد");
        }

        if (debt.IsSettled)
        {
            return (false, "این مورد قبلاً تسویه شده است");
        }

        if (amount > debt.Remaining)
        {
            return (false, "مبلغ پرداخت از مانده بیشتر است");
        }

        debt.PaidAmount += amount;

        string message;

        if (debt.Remaining <= 0)
        {
            debt.Status = DebtStatus.Settled;
            debt.SettledAt = DateTime.Now;
            message = $"تسویهٔ کامل «{debt.Title}» ثبت شد";
        }
        else
        {
            message = $"پرداخت ثبت شد. ماندهٔ «{debt.Title}»: {debt.Remaining:N0} ریال";
        }

        db.AuditLogs.Add(new AuditLog
        {
            Action = "پرداخت",
            Entity = debt.Type == DebtType.Payable ? "بدهی" : "طلب",
            EntityId = debt.Id,
            Details = $"پرداخت {amount:N0} ریال روی «{debt.Title}» — {message}",
            Username = username
        });

        await db.SaveChangesAsync();

        return (true, message);
    }

    /// <summary>سال شمسی یک تاریخ</summary>
    public static int PersianYearOf(DateTime date) => Pc.GetYear(date);

    /// <summary>تاریخ شمسی با قالب کوتاه — برای استفاده در همین بخش</summary>
    public static string Fa(DateTime date) => PersianDate.ToDate(date);
}
