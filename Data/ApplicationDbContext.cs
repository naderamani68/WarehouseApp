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
        public DbSet<ProductImage> ProductImages { get; set; }       // گالری عکس کالا

        // ===== بخش مالی =====
        public DbSet<Cheque> Cheques { get; set; }     // چک و سفته
        public DbSet<Debt> Debts { get; set; }         // بدهی و طلب
        public DbSet<Expense> Expenses { get; set; }   // هزینه‌ها

        // ===== تنظیمات =====
        public DbSet<AppSetting> AppSettings { get; set; }   // تنظیمات قابل ویرایش (نرخ مالیات، اطلاعات شرکت)
        public DbSet<SmsMessage> SmsMessages { get; set; }   // پیامک‌های ارسالی

        // 👇 متد جدید برای تنظیم رفتار حذف دسته
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // کد کالا باید یکتا باشد — ولی چند کالای بدون کد مجاز است
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Code)
                .IsUnique()
                .HasFilter("[Code] IS NOT NULL");

            // برای کالاهایی که از قبل در پایگاه داده هستند، مقدار پیش‌فرض منطقی می‌گذاریم
            // تا «غیرفعال» یا واحد صفر نشوند
            modelBuilder.Entity<Product>()
                .Property(p => p.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Product>()
                .Property(p => p.Unit)
                .HasDefaultValue(ProductUnit.Number);

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

            // ===== تنظیمات پیش‌فرض =====
            // این مقدارها در پایگاه داده ذخیره می‌شوند و از صفحهٔ «تنظیمات» قابل ویرایش‌اند
            var seedDate = new DateTime(2026, 1, 1);

            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting { Key = "Tax.VatPercent", Value = "10", Title = "نرخ مالیات بر ارزش افزوده (درصد)", Group = "مالیات", ValueType = "percent", SortOrder = 1, Hint = "نرخ قانونی روز — با تغییر قانون فقط همین عدد را عوض کن", UpdatedAt = seedDate },
                new AppSetting { Key = "Tax.VatEnabled", Value = "false", Title = "محاسبهٔ مالیات در فاکتور فروش", Group = "مالیات", ValueType = "bool", SortOrder = 2, Hint = "اگر خاموش باشد، مالیات روی فاکتور محاسبه نمی‌شود", UpdatedAt = seedDate },
                new AppSetting { Key = "Tax.VatOnPurchase", Value = "false", Title = "محاسبهٔ مالیات در خرید", Group = "مالیات", ValueType = "bool", SortOrder = 3, Hint = "مالیات پرداختی بابت خرید کالا و خدمات", UpdatedAt = seedDate },
                new AppSetting { Key = "Company.Name", Value = "", Title = "نام شرکت یا فروشگاه", Group = "اطلاعات شرکت", ValueType = "text", SortOrder = 1, Hint = "روی سرصفحهٔ فاکتور چاپ می‌شود", UpdatedAt = seedDate },
                new AppSetting { Key = "Company.Phone", Value = "", Title = "تلفن", Group = "اطلاعات شرکت", ValueType = "text", SortOrder = 2, UpdatedAt = seedDate },
                new AppSetting { Key = "Company.Address", Value = "", Title = "نشانی", Group = "اطلاعات شرکت", ValueType = "text", SortOrder = 3, UpdatedAt = seedDate },
                new AppSetting { Key = "Company.EconomicCode", Value = "", Title = "کد اقتصادی", Group = "اطلاعات شرکت", ValueType = "text", SortOrder = 4, UpdatedAt = seedDate },
                new AppSetting { Key = "Company.NationalId", Value = "", Title = "شناسهٔ ملی", Group = "اطلاعات شرکت", ValueType = "text", SortOrder = 5, UpdatedAt = seedDate },
                new AppSetting { Key = "Sales.InvoicePrefix", Value = "SO", Title = "پیشوند شمارهٔ فاکتور", Group = "فروش", ValueType = "text", SortOrder = 1, Hint = "مثلاً SO که ابتدای شمارهٔ فاکتور می‌آید", UpdatedAt = seedDate },
                new AppSetting { Key = "Sales.DefaultPaymentDays", Value = "0", Title = "مهلت پرداخت پیش‌فرض (روز)", Group = "فروش", ValueType = "number", SortOrder = 2, Hint = "برای ساخت خودکار تاریخ سررسید فاکتور", UpdatedAt = seedDate },

                // ===== پیامک =====
                new AppSetting { Key = "Sms.Enabled", Value = "false", Title = "ارسال پیامک", Group = "پیامک", ValueType = "bool", SortOrder = 1, Hint = "تا خاموش باشد هیچ پیامکی فرستاده نمی‌شود", UpdatedAt = seedDate },
                new AppSetting { Key = "Sms.ApiUrl", Value = "", Title = "نشانی پنل پیامک", Group = "پیامک", ValueType = "text", SortOrder = 2, Hint = "جانگهدارها: {apiKey} و {sender} و {receptor} و {message}", UpdatedAt = seedDate },
                new AppSetting { Key = "Sms.ApiKey", Value = "", Title = "کلید پنل پیامک", Group = "پیامک", ValueType = "text", SortOrder = 3, UpdatedAt = seedDate },
                new AppSetting { Key = "Sms.Sender", Value = "", Title = "شمارهٔ فرستنده", Group = "پیامک", ValueType = "text", SortOrder = 4, UpdatedAt = seedDate });

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
