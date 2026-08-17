using System.ComponentModel.DataAnnotations;

namespace MyConsoleApp.Server.Models;

public class SaleRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int QuantitySold { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
}
