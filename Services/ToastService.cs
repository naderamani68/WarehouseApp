namespace WarehouseApp.Services;

/// <summary>
/// نوع پیام شناور
/// </summary>
public enum ToastKind
{
    Success,
    Error,
    Info
}

/// <summary>
/// یک پیام شناور
/// </summary>
public class ToastItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Text { get; init; } = string.Empty;
    public ToastKind Kind { get; init; } = ToastKind.Success;
}

/// <summary>
/// سرویس مدیریت پیام‌های شناور (Toast).
/// این سرویس در محدودهٔ هر مدار کاربر (circuit) یک نمونه دارد،
/// پس صفحه‌ها و کامپوننت نمایش‌دهنده، همان فهرست مشترک را می‌بینند.
/// </summary>
public class ToastService
{
    private readonly List<ToastItem> _items = new();

    public IReadOnlyList<ToastItem> Items => _items;

    /// <summary>هر بار که فهرست پیام‌ها تغییر کند، این رویداد صدا زده می‌شود.</summary>
    public event Action? OnChange;

    public void ShowSuccess(string text, int durationMs = 3500) => Show(text, ToastKind.Success, durationMs);

    public void ShowError(string text, int durationMs = 6000) => Show(text, ToastKind.Error, durationMs);

    public void ShowInfo(string text, int durationMs = 4000) => Show(text, ToastKind.Info, durationMs);

    private void Show(string text, ToastKind kind, int durationMs)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var item = new ToastItem { Text = text, Kind = kind };
        _items.Add(item);
        OnChange?.Invoke();

        if (durationMs > 0)
        {
            // حذف خودکار پس از مدت مشخص (روی یک ترد آزاد اجرا می‌شود)
            _ = Task.Delay(durationMs).ContinueWith(_ => Remove(item.Id));
        }
    }

    public void Remove(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null)
        {
            return;
        }

        _items.Remove(item);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        OnChange?.Invoke();
    }
}
