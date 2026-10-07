namespace WarehouseApp.Data;

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }

    // ===== ارتباط با دسته (اختیاری) =====
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}