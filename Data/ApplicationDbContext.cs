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
        }
    }
}