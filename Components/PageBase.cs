using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WarehouseApp.Data;
using WarehouseApp.Services;

namespace WarehouseApp.Components;

/// <summary>
/// کلاس پایهٔ صفحه‌های داده.
///
/// کارهای آن:
///   ۱. خواندن اختیارات کاربر جاری از پایگاه داده
///   ۲. اگر کاربر اختیار «دیدن اطلاعات» نداشت، هدایت به صفحهٔ عدم دسترسی
///   ۳. در اختیار گذاشتن ابزار Can(...) برای پنهان‌کردن دکمه‌ها
///
/// صفحه‌ها به‌جای OnInitializedAsync، متد OnPageInitializedAsync را پیاده می‌کنند.
/// </summary>
public abstract class PageBase : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    [Inject] protected AccessService Access { get; set; } = default!;

    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Inject] protected ApplicationDbContext Db { get; set; } = default!;
    [Inject] protected IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

    /// <summary>اختیارات کاربر جاری</summary>
    protected Permissions MyPermissions { get; private set; } = Permissions.None;

    private string? currentUsername;

    /// <summary>آیا کاربر این اختیار را دارد؟</summary>
    protected bool Can(Permissions permission) => (MyPermissions & permission) == permission;

    /// <summary>آیا کاربر حداقل یکی از این اختیارات را دارد؟</summary>
    protected bool CanAny(params Permissions[] anyOf)
        => anyOf.Any(p => (MyPermissions & p) == p);

    /// <summary>ثبت یک رویداد در تاریخچهٔ عملیات</summary>
    protected async Task AuditAsync(string action, string entity, int? entityId, string details)
    {
        Db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            Username = currentUsername
        });

        await Db.SaveChangesAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        var user = AuthState is null ? null : (await AuthState).User;
        currentUsername = user?.Identity?.Name;

        // اگر رمز کاربر موقت است، اول باید آن را عوض کند
        if (await Access.MustChangePasswordAsync(user))
        {
            Nav.NavigateTo("/account/change-password?forced=1", forceLoad: true);
            return;
        }

        MyPermissions = await Access.GetPermissionsAsync(user);

        if (!Can(Permissions.ViewData))
        {
            Nav.NavigateTo("/access-denied", forceLoad: true);
            return;
        }

        await OnPageInitializedAsync();
    }

    /// <summary>جایگزین OnInitializedAsync در صفحه‌ها</summary>
    protected virtual Task OnPageInitializedAsync() => Task.CompletedTask;
}
