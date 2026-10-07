using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;

namespace WarehouseApp.Services;

/// <summary>
/// نتیجهٔ عملیات احراز هویت
/// </summary>
public class AuthResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Employee? Employee { get; init; }
}

/// <summary>
/// سرویس احراز هویت: بررسی ورود، هش‌کردن رمز و ساخت کاربر
/// </summary>
public class AuthService
{
    /// <summary>حداکثر تلاش ناموفق پیش از قفل موقت حساب</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>مدت قفل موقت حساب (دقیقه)</summary>
    public const int LockoutMinutes = 5;

    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<Employee> _hasher = new();

    public AuthService(ApplicationDbContext db) => _db = db;

    /// <summary>آیا هیچ کارمندی ثبت شده است؟ (برای تشخیص راه‌اندازی اولیه)</summary>
    public Task<bool> AnyEmployeesAsync() => _db.Employees.AnyAsync();

    /// <summary>بررسی نام کاربری و رمز عبور</summary>
    public async Task<AuthResult> ValidateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return new AuthResult { Success = false, Error = "نام کاربری و رمز عبور را وارد کنید" };
        }

        var user = await _db.Employees.FirstOrDefaultAsync(e => e.Username == username);
        if (user is null)
        {
            return new AuthResult { Success = false, Error = "نام کاربری یا رمز عبور نادرست است" };
        }

        if (!user.IsActive)
        {
            return new AuthResult { Success = false, Error = "حساب کاربری شما غیرفعال است. با مدیر سیستم تماس بگیرید" };
        }

        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.Now)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"حساب شما موقتاً قفل است. تا ساعت {user.LockoutUntil.Value:HH:mm} صبر کنید"
            };
        }

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (check == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.Now.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync();
            return new AuthResult { Success = false, Error = "نام کاربری یا رمز عبور نادرست است" };
        }

        // اگر الگوریتم هش قدیمی شده باشد، رمز را با الگوریتم تازه دوباره هش می‌کنیم
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = user };
    }

    /// <summary>ساخت اولین مدیر سیستم — فقط وقتی هیچ کارمندی وجود ندارد</summary>
    public async Task<AuthResult> CreateFirstAdminAsync(string fullName, string username, string password)
    {
        if (await AnyEmployeesAsync())
        {
            return new AuthResult { Success = false, Error = "راه‌اندازی پیش‌تر انجام شده است" };
        }

        var admin = new Employee
        {
            EmployeeCode = "1001",
            FullName = fullName.Trim(),
            Username = username.Trim(),
            Position = "مدیر سیستم",
            Role = UserRole.Admin,
            IsActive = true
        };
        admin.PasswordHash = _hasher.HashPassword(admin, password);

        _db.Employees.Add(admin);
        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = admin };
    }

    /// <summary>ساخت کارمند جدید (برای فاز مدیریت کارمندان)</summary>
    public async Task<AuthResult> CreateEmployeeAsync(Employee employee, string password)
    {
        if (await _db.Employees.AnyAsync(e => e.Username == employee.Username))
        {
            return new AuthResult { Success = false, Error = "این نام کاربری قبلاً ثبت شده است" };
        }

        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode))
        {
            return new AuthResult { Success = false, Error = "این کد پرسنلی قبلاً ثبت شده است" };
        }

        employee.PasswordHash = _hasher.HashPassword(employee, password);
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = employee };
    }

    /// <summary>هش‌کردن رمز عبور</summary>
    public string HashPassword(Employee employee, string password) => _hasher.HashPassword(employee, password);
}
