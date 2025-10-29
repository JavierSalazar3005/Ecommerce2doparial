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
        [Route("api/pedidos")]
        [Authorize(Roles = nameof(Role.Cliente) + "," + nameof(Role.AdminRoot))]
        public class OrdersController : ControllerBase
        {
            private readonly AppDbContext _db;
            private readonly OrderService _orders;

            public OrdersController(AppDbContext db, OrderService orders)
            {
                _db = db; _orders = orders;
            }

            private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            private bool IsAdmin => User.IsInRole(Role.AdminRoot.ToString());

            // =======================
            // POST /api/pedidos/lote
            // =======================
            [HttpPost("lote")]
            public async Task<ActionResult<CreateOrderBatchResponseDto>> CreateBatch(CreateOrderBatchDto dto)
            {
                if (dto?.Orders == null || dto.Orders.Count == 0)
                    return BadRequest("Debes enviar al menos un pedido.");

                var checkoutGroupId = Guid.NewGuid();
                var createdOrders = new List<Order>();

                foreach (var single in dto.Orders)
                {
                    var items = single.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

                    var (order, error) = await _orders.CreateOrderTransactionalAsync(
                        clienteId: CurrentUserId,
                        empresaId: single.EmpresaId,
                        items: items,
                        checkoutGroupId: checkoutGroupId // <- soporta agrupar
                    );

                    if (error is not null)
                        return BadRequest($"Empresa {single.EmpresaId}: {error}");

                    if (order is null)
                        return Problem($"No se pudo crear el pedido para empresa {single.EmpresaId}.");

                    createdOrders.Add(order);
                }

                var respOrders = await MapOrders(createdOrders);

                var resp = new CreateOrderBatchResponseDto
                {
                    CheckoutGroupId = checkoutGroupId,
                    Orders = respOrders
                };

                return CreatedAtAction(nameof(GetByGroup), new { groupId = checkoutGroupId }, resp);
            }

            // ======================================
            // GET /api/pedidos/grupo/{groupId:guid}
            // ======================================
            [HttpGet("grupo/{groupId:guid}")]
            public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByGroup(Guid groupId)
            {
                var orders = await _db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.CheckoutGroupId == groupId && o.ClienteId == CurrentUserId)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();

                var resp = await MapOrders(orders);
                return Ok(resp);
            }

            // ==========================
            // POST /api/pedidos
            // ==========================
            [HttpPost]
            public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderDto dto)
            {
                var items = dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

                var (order, error) = await _orders.CreateOrderTransactionalAsync(
                    clienteId: CurrentUserId,
                    empresaId: dto.EmpresaId,
                    items: items
                );

                if (error is not null) return BadRequest(error);
                if (order is null) return Problem("No se pudo crear el pedido.");

                var resp = new OrderResponseDto
                {
                    Id = order.Id,
                    EmpresaId = order.EmpresaId,
                    ClienteId = order.ClienteId,
                    Fecha = order.Fecha,
                    Status = order.Status,
                    Total = order.Total,
                    Items = order.Items.Select(oi => new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = _db.Products.Where(p => p.Id == oi.ProductId).Select(p => p.Name).FirstOrDefault() ?? "",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Subtotal = oi.Subtotal
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetById), new { id = order.Id }, resp);
            }

            // ===================================
            // GET /api/pedidos/mios?empresaId=&status=
            // ===================================
            [HttpGet("mios")]
            public async Task<ActionResult<IEnumerable<OrderResponseDto>>> MyOrders(
                [FromQuery] int? empresaId,
                [FromQuery] OrderStatus? status)
            {
                var query = _db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.ClienteId == CurrentUserId);

                if (empresaId.HasValue)
                    query = query.Where(o => o.EmpresaId == empresaId.Value);

                if (status.HasValue)
                    query = query.Where(o => o.Status == status.Value);

                var list = await query.OrderByDescending(o => o.Id).ToListAsync();
                var map = await MapOrders(list);
                return Ok(map);
            }

            // ==========================
            // GET /api/pedidos/{id}
            // ==========================
            [HttpGet("{id}")]
            public async Task<ActionResult<OrderResponseDto>> GetById(int id)
            {
                var order = await _db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order is null) return NotFound();
                if (!IsAdmin && order.ClienteId != CurrentUserId) return Forbid();

                var resp = (await MapOrders(new List<Order> { order })).First();
                return Ok(resp);
            }

            // ==========================
            // Mapper
            // ==========================
            private async Task<List<OrderResponseDto>> MapOrders(List<Order> orders)
            {
                var productNames = await _db.Products
                    .Where(p => orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name);

                return orders.Select(o => new OrderResponseDto
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
                }).ToList();
            }
        }
    }
