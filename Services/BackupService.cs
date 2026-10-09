using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// پشتیبان‌گیری از پایگاه داده و عکس‌های کالا.
///
/// نام فایل پشتیبان با تاریخ میلادی ساخته می‌شود تا مرتب‌شدن فایل‌ها ساده باشد،
/// ولی در صفحهٔ پشتیبان‌گیری تاریخ شمسی به کاربر نشان داده می‌شود.
///
/// نکتهٔ مهم اول: پشتیبان پایگاه داده، عکس‌ها را شامل نمی‌شود؛ پس هر دو با هم گرفته می‌شوند.
///
/// نکتهٔ مهم دوم: سرویس پایگاه داده معمولاً اجازهٔ نوشتن در پوشه‌های کاربر را ندارد.
/// پس اول پشتیبان‌گیری مستقیم در پوشهٔ انتخابی آزمایش می‌شود؛ اگر نشد،
/// فایل در پوشهٔ پیش‌فرض خود پایگاه داده ساخته و سپس به پوشهٔ انتخابی کپی می‌شود.
/// </summary>
public class BackupService
{
    public const string FolderKey = "Backup.Folder";
    public const string KeepDaysKey = "Backup.KeepDays";

    private readonly ApplicationDbContext db;
    private readonly IWebHostEnvironment env;
    private readonly ILogger<BackupService> logger;

    private string lastError = string.Empty;

    public BackupService(ApplicationDbContext db, IWebHostEnvironment env, ILogger<BackupService> logger)
    {
        this.db = db;
        this.env = env;
        this.logger = logger;
    }

    /// <summary>پوشهٔ پیش‌فرض — کنار خود برنامه</summary>
    public string DefaultFolder => Path.Combine(env.ContentRootPath, "backups");

    /// <summary>مسیر اسکریپت پشتیبان‌گیری برای زمان‌بندی ویندوز</summary>
    public string ScriptPath => Path.Combine(env.ContentRootPath, "scripts", "backup-database.ps1");

    public async Task<string> GetFolderAsync()
    {
        var value = await db.AppSettings
            .Where(s => s.Key == FolderKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(value) ? DefaultFolder : value.Trim();
    }

    public async Task<int> GetKeepDaysAsync()
    {
        var value = await db.AppSettings
            .Where(s => s.Key == KeepDaysKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var days) && days >= 0
            ? days
            : 30;
    }

    public async Task SaveSettingsAsync(string? folder, int keepDays)
    {
        await SetAsync(FolderKey, folder?.Trim() ?? string.Empty);
        await SetAsync(KeepDaysKey, Math.Max(0, keepDays).ToString(CultureInfo.InvariantCulture));
        await db.SaveChangesAsync();
    }

    private async Task SetAsync(string key, string value)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Title = key,
                Group = "پشتیبان‌گیری",
                ValueType = "text",
                SortOrder = 90,
                UpdatedAt = DateTime.Now
            });
        }
        else
        {
            row.Value = value;
            row.UpdatedAt = DateTime.Now;
        }
    }

    /// <summary>فهرست پشتیبان‌های موجود، از تازه به قدیم</summary>
    public List<BackupFile> ListBackups(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new List<BackupFile>();
        }

        return Directory.GetFiles(folder, "WarehouseAppDb_*.bak")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupFile(f.Name, f.FullName, f.Length, f.CreationTime))
            .ToList();
    }

    public async Task<BackupResult> RunAsync()
    {
        try
        {
            var folder = await GetFolderAsync();
            var keepDays = await GetKeepDaysAsync();

            Directory.CreateDirectory(folder);

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var file = Path.Combine(folder, $"WarehouseAppDb_{stamp}.bak");

            var database = db.Database.GetDbConnection().Database;
            var safeDatabase = Escape(database.Replace("]", "]]"));

            // پایگاه دادهٔ بزرگ می‌تواند بیشتر از مهلت پیش‌فرض طول بکشد
            db.Database.SetCommandTimeout(900);

            if (!await TryBackupAsync(safeDatabase, Escape(file)))
            {
                // سرویس پایگاه داده معمولاً اجازهٔ نوشتن در پوشهٔ کاربر را ندارد.
                // یک بار دسترسی نوشتن به حساب آن سرویس داده می‌شود و دوباره تلاش می‌کنیم.
                var account = await GetSqlServiceAccountAsync();
                var granted = await GrantAccessAsync(folder, account);

                if (granted)
                {
                    await TryBackupAsync(safeDatabase, Escape(file));
                }

                if (!File.Exists(file))
                {
                    var hint = granted
                        ? "دوباره تلاش کن؛ اگر باز هم نشد، با دسترسی مدیر این دستور را اجرا کن:"
                        : "با دسترسی مدیر این دستور را اجرا کن:";

                    return new BackupResult(
                        false,
                        $"پشتیبان‌گیری ناموفق بود — {Shorten(lastError)}\n{hint}\nicacls \"{folder}\" /grant \"{account}:(OI)(CI)M\"",
                        folder,
                        null,
                        0,
                        0,
                        0);
                }
            }

            if (!File.Exists(file))
            {
                return new BackupResult(false, "دستور پشتیبان اجرا شد ولی فایلی ساخته نشد. دسترسی پوشه را بررسی کن.", folder, null, 0, 0, 0);
            }

            var size = new FileInfo(file).Length;

            // ===== عکس‌های کالا =====
            var uploads = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
            var copied = 0;

            if (Directory.Exists(uploads))
            {
                var target = Path.Combine(folder, $"uploads_{stamp}");
                CopyFolder(uploads, target);
                copied = Directory.GetFiles(target, "*", SearchOption.AllDirectories).Length;
            }

            var deleted = RemoveOld(folder, keepDays);

            logger.LogInformation("پشتیبان‌گیری انجام شد: {File} ({Size} بایت)", file, size);

            return new BackupResult(true, "پشتیبان با موفقیت گرفته شد", folder, file, size, copied, deleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در پشتیبان‌گیری");
            return new BackupResult(false, ex.Message, null, null, 0, 0, 0);
        }
    }

    /// <summary>یک بار تلاش برای پشتیبان‌گیری؛ در صورت خطا، پیام را نگه می‌دارد</summary>
    private async Task<bool> TryBackupAsync(string safeDatabase, string safeFile)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{safeDatabase}] TO DISK = N'{safeFile}' WITH INIT, COMPRESSION");

            return true;
        }
        catch (Exception ex)
        {
            lastError = ex.Message;

            // اتصال پس از خطا ممکن است خراب بماند؛ برای تلاش بعدی تازه‌سازی می‌کنیم
            try
            {
                await db.Database.CloseConnectionAsync();
            }
            catch
            {
                // بی‌اهمیت
            }

            return false;
        }
    }

    /// <summary>حساب کاربری سرویس پایگاه داده — برای دادن دسترسی نوشتن روی پوشهٔ پشتیبان</summary>
    private async Task<string> GetSqlServiceAccountAsync()
    {
        try
        {
            var value = (await db.Database
                .SqlQueryRaw<string>("SELECT TOP 1 CAST(service_account AS nvarchar(4000)) AS [Value] FROM sys.dm_server_services WHERE servicename LIKE 'SQL Server (%'")
                .ToListAsync())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        catch
        {
            // ادامه با حساب پیش‌فرض
        }

        return @"NT SERVICE\MSSQLSERVER";
    }

    /// <summary>دادن دسترسی نوشتن به حساب سرویس پایگاه داده روی پوشهٔ پشتیبان</summary>
    private static async Task<bool> GrantAccessAsync(string folder, string account)
    {
        try
        {
            var startInfo = new ProcessStartInfo("icacls", $"\"{folder}\" /grant \"{account}:(OI)(CI)M\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Shorten(string text)
        => text.Length <= 200 ? text : text[..200] + "…";

    private static void CopyFolder(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, target));
        }

        foreach (var item in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(item, item.Replace(source, target), true);
        }
    }

    /// <summary>پاک‌کردن پشتیبان‌های قدیمی‌تر از تعداد روز نگهداری</summary>
    private static int RemoveOld(string folder, int keepDays)
    {
        if (keepDays <= 0)
        {
            return 0;
        }

        var limit = DateTime.Now.AddDays(-keepDays);
        var count = 0;

        foreach (var file in Directory.GetFiles(folder, "WarehouseAppDb_*.bak"))
        {
            var info = new FileInfo(file);

            if (info.CreationTime < limit)
            {
                info.Delete();
                count++;
            }
        }

        foreach (var dir in Directory.GetDirectories(folder, "uploads_*"))
        {
            var info = new DirectoryInfo(dir);

            if (info.CreationTime < limit)
            {
                info.Delete(true);
                count++;
            }
        }

        return count;
    }
}

/// <summary>یک فایل پشتیبان</summary>
public record BackupFile(string Name, string FullPath, long Size, DateTime CreatedAt);

/// <summary>نتیجهٔ یک بار پشتیبان‌گیری</summary>
public record BackupResult(
    bool Success,
    string Message,
    string? Folder,
    string? FilePath,
    long Size,
    int UploadsCopied,
    int OldRemoved);
