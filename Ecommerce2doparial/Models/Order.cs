using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce2doparial.Models;

public class Order
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public User Cliente { get; set; } = null!;

    public int EmpresaId { get; set; } // pedidos por empresa
    public User Empresa { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Nuevo;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    
    public Guid? CheckoutGroupId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}