using System.Security.Claims;
using Ecommerce2doparial.Data;
using Ecommerce2doparial.DTOs;
using Ecommerce2doparial.Models;
using Ecommerce2doparial.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Controllers

{
    [ApiController]
    [Route("api/empresa/pedidos")]
    [Authorize(Roles = nameof(Role.Empresa) + "," + nameof(Role.AdminRoot))]
    public class EmpresaOrdersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly OrderService _orders;

        public EmpresaOrdersController(AppDbContext db, OrderService orders)
        {
            _db = db; _orders = orders;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin => User.IsInRole(Role.AdminRoot.ToString());

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> List()
        {
            var query = _db.Orders.Include(o => o.Items).AsQueryable();
            if (!IsAdmin) query = query.Where(o => o.EmpresaId == CurrentUserId);

            var orders = await query.OrderByDescending(o => o.Id).ToListAsync();

            var productNames = await _db.Products
                .Where(p => orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var resp = orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                EmpresaId = o.EmpresaId,
                ClienteId = o.ClienteId,
                Fecha = o.Fecha,
                Status = o.Status,
                Total = o.Total,
                Items = o.Items.Select(i => new OrderItemResponseDto
                {
                    ProductId = i.ProductId,
                    ProductName = productNames.TryGetValue(i.ProductId, out var name) ? name : "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal
                }).ToList()
            });

            return Ok(resp);
        }

        [HttpPatch("{id}/estado")]
        public async Task<ActionResult<OrderResponseDto>> ChangeStatus(int id, UpdateOrderStatusDto dto)
        {
            int empresaId = CurrentUserId;
            if (IsAdmin)
            {
                // Si es admin, debemos obtener el EmpresaId real del pedido
                var eId = await _db.Orders.Where(o => o.Id == id).Select(o => o.EmpresaId).FirstOrDefaultAsync();
                if (eId == 0) return NotFound("Pedido no encontrado.");
                empresaId = eId;
            }

            var (order, error) = await _orders.ChangeStatusAsync(empresaId, id, dto.Status);
            if (error is not null) return BadRequest(error);
            if (order is null) return Problem("No se pudo cambiar el estado.");

            // map mínimo
            return new OrderResponseDto
            {
                Id = order.Id,
                EmpresaId = order.EmpresaId,
                ClienteId = order.ClienteId,
                Fecha = order.Fecha,
                Status = order.Status,
                Total = order.Total,
                Items = order.Items.Select(i => new OrderItemResponseDto
                {
                    ProductId = i.ProductId,
                    ProductName = _db.Products.Where(p => p.Id == i.ProductId).Select(p => p.Name).FirstOrDefault() ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal
                }).ToList()
            };
        }
    }
}