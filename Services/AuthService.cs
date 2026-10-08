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

        var user = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Username == username);
        if (user is null)
        {
            return new AuthResult { Success = false, Error = "نام کاربری یا رمز عبور نادرست است" };
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

        // ===== بررسی وضعیت حساب — بعد از تأیید رمز، تا اطلاعات حساب لو نرود =====
        if (!user.IsActive)
        {
            return new AuthResult { Success = false, Error = "حساب کاربری شما غیرفعال است. با مدیر سیستم تماس بگیرید" };
        }

        if (user.Role is not null && !user.Role.IsActive)
        {
            return new AuthResult { Success = false, Error = "نقش (سمت) شما غیرفعال شده است. با مدیر سیستم تماس بگیرید" };
        }

        // اگر الگوریتم هش قدیمی شده باشد، رمز را با الگوریتم تازه دوباره هش می‌کنیم
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.Now;

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "ورود",
            Entity = "حساب کاربری",
            EntityId = user.Id,
            Details = $"کاربر «{user.FullName}» وارد سیستم شد",
            Username = user.Username
        });

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
            RoleId = Role.AdminRoleId,
            IsActive = true
        };
        if (!IsLatinPassword(password))
        {
            return new AuthResult { Success = false, Error = "رمز عبور باید فقط شامل حروف، عدد و نشانه‌های انگلیسی باشد" };
        }

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

        if (!IsLatinPassword(password))
        {
            return new AuthResult { Success = false, Error = "رمز عبور باید فقط شامل حروف، عدد و نشانه‌های انگلیسی باشد" };
        }

        employee.PasswordHash = _hasher.HashPassword(employee, password);
        employee.MustChangePassword = true;   // در اولین ورود، رمز را عوض کند
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = employee };
    }

    /// <summary>به‌روزرسانی اطلاعات کارمند و (در صورت نیاز) بازنشانی رمز عبور</summary>
    public async Task<AuthResult> UpdateEmployeeAsync(Employee employee, string? newPassword)
    {
        if (await _db.Employees.AnyAsync(e => e.Username == employee.Username && e.Id != employee.Id))
        {
            return new AuthResult { Success = false, Error = "این نام کاربری برای کارمند دیگری ثبت شده است" };
        }

        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode && e.Id != employee.Id))
        {
            return new AuthResult { Success = false, Error = "این کد پرسنلی برای کارمند دیگری ثبت شده است" };
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (!IsLatinPassword(newPassword))
            {
                return new AuthResult { Success = false, Error = "رمز عبور باید فقط شامل حروف، عدد و نشانه‌های انگلیسی باشد" };
            }

            employee.PasswordHash = _hasher.HashPassword(employee, newPassword);
            employee.MustChangePassword = true;   // کاربر باید رمز جدید را خودش تعیین کند
        }

        _db.Employees.Update(employee);
        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = employee };
    }

    /// <summary>آیا رمز فقط شامل کاراکترهای قابل‌چاپ انگلیسی است؟ (بدون فاصله)</summary>
    private static bool IsLatinPassword(string password)
        => !string.IsNullOrEmpty(password) && password.All(c => c >= 33 && c <= 126);

    /// <summary>تغییر رمز عبور توسط خود کاربر (با بررسی رمز فعلی)</summary>
    public async Task<AuthResult> ChangePasswordAsync(int employeeId, string currentPassword, string newPassword)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee is null)
        {
            return new AuthResult { Success = false, Error = "کاربر یافت نشد" };
        }

        var check = _hasher.VerifyHashedPassword(employee, employee.PasswordHash, currentPassword);
        if (check == PasswordVerificationResult.Failed)
        {
            return new AuthResult { Success = false, Error = "رمز عبور فعلی نادرست است" };
        }

        if (!IsLatinPassword(newPassword))
        {
            return new AuthResult { Success = false, Error = "رمز عبور باید فقط شامل حروف، عدد و نشانه‌های انگلیسی باشد" };
        }

        employee.PasswordHash = _hasher.HashPassword(employee, newPassword);
        employee.MustChangePassword = false;

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "ویرایش",
            Entity = "حساب کاربری",
            EntityId = employee.Id,
            Details = "رمز عبور توسط خود کاربر تغییر کرد",
            Username = employee.Username
        });

        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = employee };
    }

    /// <summary>ویرایش اطلاعات تماس توسط خود کاربر</summary>
    public async Task<AuthResult> UpdateOwnContactAsync(int employeeId, string? phone, string? address)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee is null)
        {
            return new AuthResult { Success = false, Error = "کاربر یافت نشد" };
        }

        employee.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        employee.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "ویرایش",
            Entity = "پروفایل",
            EntityId = employee.Id,
            Details = "اطلاعات تماس توسط خود کاربر ویرایش شد",
            Username = employee.Username
        });

        await _db.SaveChangesAsync();

        return new AuthResult { Success = true, Employee = employee };
    }

    /// <summary>هش‌کردن رمز عبور</summary>
    public string HashPassword(Employee employee, string password) => _hasher.HashPassword(employee, password);
}
