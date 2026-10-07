using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Data
{
    public enum MovementType
    {
        [Display(Name = "ورود")]
        In = 1,

        [Display(Name = "خروج")]
        Out = 2
    }

    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public MovementType Type { get; set; }

        public int Quantity { get; set; }

        public DateTime MovementDate { get; set; } = DateTime.Now;

        public string? Note { get; set; }
    }
}