using Microsoft.EntityFrameworkCore;
using WarehouseApp.Components;
using WarehouseApp.Data;
using WarehouseApp.Services;

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<ThemeService>();

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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
