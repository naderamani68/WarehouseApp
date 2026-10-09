using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WarehouseApp.Components;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WarehouseApp.Data;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WarehouseApp.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ===== تنظیم لاگ‌گیری =====
// نویسندهٔ پیش‌فرض «وقایع ویندوز» روی این سیستم اجازهٔ نوشتن ندارد و هنگام
// ثبت خطا، خودش برنامه را از کار می‌انداخت؛ پس آن را برمی‌داریم.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// کارخانهٔ ساخت زمینهٔ پایگاه داده.
// چون چیدمان (منو) و صفحه هم‌زمان رندر می‌شوند، هر عملیات باید زمینهٔ مستقل خودش را
// داشته باشد؛ در غیر این صورت خطای «دو عملیات هم‌زمان روی یک اتصال» رخ می‌دهد.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// زمینهٔ درون‌درخواستی برای صفحه‌ها (هر درخواست یک نمونه)
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ShopCatalogService>();
// ===== محدودسازی نرخ درخواست =====
// فروشگاه عمومی سهم کمتری دارد تا یک ربات نتواند پنل انبار را از کار بیندازد.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var isShop = context.Request.Path.StartsWithSegments("/shop");
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            (isShop ? "shop:" : "panel:") + ip,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isShop ? 120 : 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// ===== احراز هویت با کوکی =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.Cookie.Name = "WarehouseApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // ===== اعتبارسنجی مجدد کوکی در هر درخواست =====
        // اگر کارمند یا نقش (سمت) او غیرفعال شود، در همان درخواست بعدی از سیستم خارج می‌شود.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var employeeId = AccessService.GetEmployeeId(context.Principal);

                if (employeeId is null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var factory = context.HttpContext.RequestServices
                    .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

                await using var db = await factory.CreateDbContextAsync();

                var employee = await db.Employees
                    .Include(e => e.Role)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                // دلیل خارج‌شدن را در یک کوکی کوتاه‌عمر می‌گذاریم تا صفحهٔ ورود نشان دهد
                string? notice = null;

                if (employee is null)
                {
                    notice = "not-found";
                }
                else if (!employee.IsActive)
                {
                    notice = "employee-inactive";
                }
                else if (employee.Role is null || !employee.Role.IsActive)
                {
                    notice = "role-inactive";
                }

                if (notice is not null)
                {
                    context.Response.Cookies.Append("AuthNotice", notice, new CookieOptions
                    {
                        HttpOnly = true,
                        MaxAge = TimeSpan.FromMinutes(2)
                    });

                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    })
    // ===== کوکی جداگانهٔ مشتریان فروشگاه =====
    // مسیر کوکی روی «/shop» محدود است، پس مرورگر آن را به پنل انبار نمی‌فرستد.
    // نتیجه: حتی اگر یک ایراد امنیتی در فروشگاه پیدا شود، نشست کارمندان در خطر نیست.
    .AddCookie(ShopAuth.Scheme, options =>
    {
        options.LoginPath = "/shop/account/login";
        options.LogoutPath = "/shop/account/logout";
        options.AccessDeniedPath = "/shop";
        options.Cookie.Name = "WarehouseApp.Shop";
        options.Cookie.Path = "/shop";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // سیاست مخصوص مشتریان فروشگاه؛ با طرح ورود جداگانه کار می‌کند
    options.AddPolicy(ShopAuth.CustomerPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(ShopAuth.Scheme);
        policy.RequireAuthenticatedUser();
    });
});
builder.Services.AddCascadingAuthenticationState();

// ===== سرویس احراز هویت =====
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddHttpContextAccessor();   // برای خواندن کاربر جاری در چیدمان
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// ===== سرصفحه‌های امنیتی =====
// جلوی حدس‌زدن نوع فایل، قاب‌گرفتن سایت در صفحهٔ دیگران و ارسال نشانی صفحه به سایت‌های بیرونی را می‌گیرد.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    await next();
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ===== نقطه‌های ورود فروشگاه (سبد خرید و ثبت سفارش) =====
app.MapShopEndpoints();

app.Run();
