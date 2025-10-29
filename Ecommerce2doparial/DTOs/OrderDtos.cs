using System.ComponentModel.DataAnnotations;
using Ecommerce2doparial.Models;

namespace Ecommerce2doparial.DTOs

{
    namespace Ecommerce2doparial.DTOs
    {
        public class CreateOrderBatchDto
        {
            public List<CreateOrderDto> Orders { get; set; } = new();
        }

        public class CreateOrderBatchResponseDto
        {
            public Guid CheckoutGroupId { get; set; }
            public List<OrderResponseDto> Orders { get; set; } = new();
        }
    }
    
    // Para crear varios pedidos en un solo request
    public class CreateOrderBatchDto
    {
        public List<CreateOrderDto> Orders { get; set; } = new();
    }

// Respuesta del batch: todos los pedidos + un ID de grupo
    public class CreateOrderBatchResponseDto
    {
        public Guid CheckoutGroupId { get; set; }
        public List<OrderResponseDto> Orders { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        [Required] public int ProductId { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        [Required] public int EmpresaId { get; set; }
        [Required, MinLength(1)] public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class UpdateOrderStatusDto
    {
        [Required] public OrderStatus Status { get; set; }
    }
}