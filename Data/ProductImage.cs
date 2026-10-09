namespace WarehouseApp.Data;

/// <summary>
/// عکس گالری یک کالا — هر کالا می‌تواند چند عکس داشته باشد (حداکثر ۵)
/// </summary>
public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>مسیر نسبی عکس در پوشهٔ wwwroot</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>ترتیب نمایش در گالری</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
