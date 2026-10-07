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

        // ارتباط یک-به-چند با Product
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}