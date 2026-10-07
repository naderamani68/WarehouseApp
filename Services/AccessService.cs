using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// بررسی دسترسی کاربر جاری.
///
/// نکتهٔ مهم: اختیارات هر بار از «پایگاه داده» خوانده می‌شوند، نه از کوکی.
/// یعنی اگر مدیر اختیار یک نقش را عوض کند، همان لحظه برای همه اثر می‌کند و
/// کاربران لازم نیست خارج و دوباره وارد شوند.
/// </summary>
public class AccessService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public AccessService(IDbContextFactory<ApplicationDbContext> factory) => _factory = factory;

    /// <summary>شناسهٔ کارمند از روی اطلاعات ورود</summary>
    public static int? GetEmployeeId(ClaimsPrincipal? user)
        => int.TryParse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>فهرست اختیارات کاربر جاری</summary>
    public async Task<Permissions> GetPermissionsAsync(ClaimsPrincipal? user)
    {
        var id = GetEmployeeId(user);
        if (id is null)
        {
            return Permissions.None;
        }

        // برای هر بررسی، یک زمینهٔ مستقل ساخته می‌شود تا با سایر بخش‌ها تضاد پیدا نکند
        await using var db = await _factory.CreateDbContextAsync();

        // اگر خودِ کارمند یا نقشش غیرفعال باشد، هیچ اختیاری ندارد
        return await db.Employees
            .Where(e => e.Id == id && e.IsActive && e.Role != null && e.Role.IsActive)
            .Select(e => e.Role!.Permissions)
            .FirstOrDefaultAsync();
    }

    /// <summary>آیا کاربر جاری این اختیار را دارد؟</summary>
    public async Task<bool> HasAsync(ClaimsPrincipal? user, Permissions needed)
    {
        if (needed == Permissions.None)
        {
            return true;
        }

        var permissions = await GetPermissionsAsync(user);
        return (permissions & needed) == needed;
    }

    /// <summary>آیا کاربر جاری یکی از این اختیارات را دارد؟</summary>
    public async Task<bool> HasAnyAsync(ClaimsPrincipal? user, params Permissions[] anyOf)
    {
        var permissions = await GetPermissionsAsync(user);
        return anyOf.Any(p => (permissions & p) == p);
    }
}
