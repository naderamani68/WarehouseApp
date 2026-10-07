namespace WarehouseApp.Data
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}