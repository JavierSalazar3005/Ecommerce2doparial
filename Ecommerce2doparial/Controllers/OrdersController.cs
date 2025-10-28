namespace Ecommerce2doparial.Controllers;

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

        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderDto dto)
        {
            // Si mezclaran productos de distintas empresas, aquí se debería rechazar o dividir. Rechazamos por diseño.
            var items = dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

            var (order, error) = await _orders.CreateOrderTransactionalAsync(
                clienteId: CurrentUserId,
                empresaId: dto.EmpresaId,
                items: items
            );

            if (error is not null) return BadRequest(error);
            if (order is null) return Problem("No se pudo crear el pedido.");

            // Map response
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

        [HttpGet("mios")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> MyOrders()
        {
            var query = _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.ClienteId == CurrentUserId)
                .OrderByDescending(o => o.Id);

            var list = await query.ToListAsync();
            var map = await MapOrders(list);
            return Ok(map);
        }

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