using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Data
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام دسته الزامی است")]
        [MaxLength(100, ErrorMessage = "نام دسته نمی‌تواند بیش از ۱۰۰ کاراکتر باشد")]
        [Display(Name = "نام دسته")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "توضیحات نمی‌تواند بیش از ۵۰۰ کاراکتر باشد")]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>دستهٔ مادر — خالی یعنی این دسته، دستهٔ اصلی است</summary>
        [Display(Name = "دستهٔ مادر")]
        public int? ParentId { get; set; }

        public Category? Parent { get; set; }

        /// <summary>زیردسته‌ها</summary>
        public ICollection<Category> Children { get; set; } = new List<Category>();

        // ارتباط یک-به-چند با Product
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}