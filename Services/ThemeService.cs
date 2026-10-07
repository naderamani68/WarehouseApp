using Microsoft.JSInterop;

namespace WarehouseApp.Services;

/// <summary>
/// سرویس مدیریت تم (روشن/تیره)
/// - تم فعلی رو نگه می‌داره
/// - توی localStorage ذخیره می‌کنه
/// - به کامپوننت‌ها خبر می‌ده که تم عوض شده
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _js;

    /// <summary>
    /// تم فعلی: "light" یا "dark"
    /// </summary>
    public string CurrentTheme { get; private set; } = "light";

    /// <summary>
    /// وقتی تم عوض می‌شه، این event فعال می‌شه
    /// کامپوننت‌ها می‌تونن بهش گوش بدن
    /// </summary>
    public event Action? OnChange;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// موقع لود اولیه: تم ذخیره‌شده رو از localStorage می‌خونه و اعمال می‌کنه
    /// </summary>
    public async Task InitializeAsync()
    {
        var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "theme");
        CurrentTheme = string.IsNullOrEmpty(saved) ? "light" : saved;
        await ApplyAsync(CurrentTheme);
    }

    /// <summary>
    /// تم رو بین روشن و تیره عوض می‌کنه
    /// </summary>
    public async Task ToggleAsync()
    {
        CurrentTheme = CurrentTheme == "light" ? "dark" : "light";
        await ApplyAsync(CurrentTheme);
        OnChange?.Invoke();
    }

    /// <summary>
    /// تم رو روی صفحه اعمال می‌کنه و توی localStorage ذخیره می‌کنه
    /// </summary>
    private async Task ApplyAsync(string theme)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", "theme", theme);
        await _js.InvokeVoidAsync("eval",
            $"document.documentElement.setAttribute('data-theme','{theme}')");
    }
}