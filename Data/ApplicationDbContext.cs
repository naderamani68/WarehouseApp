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
        public DbSet<Party> Parties { get; set; }         // مشتریان و تأمین‌کنندگان

        // ===== بخش CRM =====
        public DbSet<CrmInteraction> CrmInteractions { get; set; }   // پیگیری‌ها
        public DbSet<CrmOpportunity> CrmOpportunities { get; set; }  // فرصت‌های فروش
        public DbSet<SalesOrder> SalesOrders { get; set; }           // سفارش‌های فروش
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }   // اقلام سفارش

        // ===== بخش مالی =====
        public DbSet<Cheque> Cheques { get; set; }     // چک و سفته
        public DbSet<Debt> Debts { get; set; }         // بدهی و طلب
        public DbSet<Expense> Expenses { get; set; }   // هزینه‌ها

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

            // کد طرف حساب باید یکتا باشد
            modelBuilder.Entity<Party>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // ارتباط کارمند با نقش — اگر نقش حذف شود، نقش کارمند خالی می‌شود
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== CRM =====

            // پیگیری‌ها و فرصت‌ها همراه طرف حساب پاک می‌شوند
            modelBuilder.Entity<CrmInteraction>()
                .HasOne(i => i.Party)
                .WithMany()
                .HasForeignKey(i => i.PartyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CrmOpportunity>()
                .HasOne(o => o.Party)
                .WithMany()
                .HasForeignKey(o => o.PartyId)
                .OnDelete(DeleteBehavior.Cascade);

            // شمارهٔ سفارش یکتا است
            modelBuilder.Entity<SalesOrder>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // طرف حسابی که سفارش دارد پاک نمی‌شود (سابقهٔ مالی حفظ می‌شود)
            modelBuilder.Entity<SalesOrder>()
                .HasOne(o => o.Party)
                .WithMany()
                .HasForeignKey(o => o.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            // اگر انبار حذف شود، سفارش می‌ماند ولی انبارش خالی می‌شود
            modelBuilder.Entity<SalesOrder>()
                .HasOne(o => o.Warehouse)
                .WithMany()
                .HasForeignKey(o => o.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // اقلام همراه سفارش پاک می‌شوند
            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // کالایی که در سفارش آمده پاک نمی‌شود
            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== مالی =====

            // سند مالی و بدهی با حذف طرف حساب پاک نمی‌شوند؛ فقط ارتباطشان خالی می‌شود
            modelBuilder.Entity<Cheque>()
                .HasOne(c => c.Party)
                .WithMany()
                .HasForeignKey(c => c.PartyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Debt>()
                .HasOne(d => d.Party)
                .WithMany()
                .HasForeignKey(d => d.PartyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Party)
                .WithMany()
                .HasForeignKey(e => e.PartyId)
                .OnDelete(DeleteBehavior.SetNull);

            // کالای مرتبط با هزینه، پاک نمی‌شود
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            // جستجو بر اساس تاریخ سررسید اسناد و تاریخ هزینه
            modelBuilder.Entity<Cheque>()
                .HasIndex(c => c.DueDate);

            modelBuilder.Entity<Expense>()
                .HasIndex(e => e.At);

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
                    Description = "همهٔ عملیات انبار، گزارش‌ها و CRM، بدون مدیریت کارمندان و نقش‌ها",
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
