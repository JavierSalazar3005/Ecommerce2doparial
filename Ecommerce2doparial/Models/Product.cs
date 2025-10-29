using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce2doparial.Models;

public class Product
{
    public int Id { get; set; }

    // Dueño del producto (Empresa)
    public int EmpresaId { get; set; }
    public User Empresa { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;

    // Concurrency token para evitar overselling
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}