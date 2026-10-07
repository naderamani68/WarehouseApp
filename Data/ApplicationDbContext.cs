using Microsoft.EntityFrameworkCore;

namespace WarehouseApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Category> Categories { get; set; }    // 👈 خط جدید
        public DbSet<Employee> Employees { get; set; }   // کارمندان
        public DbSet<AuditLog> AuditLogs { get; set; }   // تاریخچهٔ عملیات
        public DbSet<Role> Roles { get; set; }           // نقش‌ها و اختیارات

        // 👇 متد جدید برای تنظیم رفتار حذف دسته
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // نام کاربری و کد پرسنلی هر کارمند باید یکتا باشد
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Username)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();
            // نام نقش باید یکتا باشد
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // ارتباط کارمند با نقش — اگر نقش حذف شود، نقش کارمند خالی می‌شود
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== نقش‌های پیش‌فرض =====
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "مدیر سیستم",
                    Description = "دسترسی کامل به همهٔ بخش‌ها — شامل اختیاراتی که در آینده اضافه می‌شوند",
                    IsSystemRole = true,
                    IsActive = true,
                    // همهٔ بیت‌ها روشن: پس هر اختیار جدیدی که بعداً اضافه شود، خودکار دارد
                    Permissions = (Permissions)(-1)
                },
                new Role
                {
                    Id = 2,
                    Name = "مدیر انبار",
                    Description = "همهٔ عملیات انبار و گزارش‌ها، بدون مدیریت کارمندان",
                    IsSystemRole = true,
                    IsActive = true,
                    Permissions = Permissions.All & ~Permissions.ManageEmployees & ~Permissions.ManageRoles
                },
                new Role
                {
                    Id = 3,
                    Name = "انباردار",
                    Description = "ثبت حرکات، مدیریت محصولات و دیدن گزارش‌ها",
                    IsSystemRole = true,
                    IsActive = true,
                    Permissions = Permissions.ViewData | Permissions.ManageProducts | Permissions.RegisterMovements | Permissions.ExportExcel | Permissions.ViewReports
                },
                new Role
                {
                    Id = 4,
                    Name = "بازدیدکننده",
                    Description = "فقط مشاهده",
                    IsSystemRole = true,
                    IsActive = true,
                    Permissions = Permissions.ViewData | Permissions.ViewReports
                });
        }
    }
}