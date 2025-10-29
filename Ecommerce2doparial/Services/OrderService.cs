using System.Data;
using Ecommerce2doparial.Data;
using Ecommerce2doparial.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Services
{
    public class OrderService
    {
        private readonly AppDbContext _db;
        public OrderService(AppDbContext db) { _db = db; }

        /// <summary>
        /// Crea un pedido para una empresa y descuenta stock de forma transaccional.
        /// Usa aislamiento SERIALIZABLE para evitar carreras sin requerir RowVersion.
        /// </summary>
        public async Task<(Order? order, string? error)> CreateOrderTransactionalAsync(
            int clienteId,
            int empresaId,
            List<(int productId, int quantity)> items,
            Guid? checkoutGroupId = null,
            CancellationToken ct = default)
        {
            // 0) Validaciones básicas
            if (items == null || items.Count == 0)
                return (null, "Debes incluir al menos un item.");

            // Normaliza: agrega cantidades por ProductId y descarta <=0
            var normalized = items
                .GroupBy(x => x.productId)
                .Select(g => (productId: g.Key, quantity: g.Sum(v => v.quantity)))
                .Where(x => x.quantity > 0)
                .ToList();

            if (normalized.Count == 0)
                return (null, "Todas las cantidades son inválidas (<= 0).");

            // 1) Verifica que la empresa exista y sea Role.Empresa
            var empresaExists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == empresaId && u.Role == Role.Empresa, ct);

            if (!empresaExists)
                return (null, "Empresa no válida.");

            // 2) Carga productos de esa empresa involucrados
            var prodIds = normalized.Select(i => i.productId).ToList();

            // AsTracking para que EF detecte cambios de stock
            var products = await _db.Products
                .AsTracking()
                .Where(p => prodIds.Contains(p.Id) && p.EmpresaId == empresaId && p.IsActive)
                .ToListAsync(ct);

            if (products.Count != prodIds.Count)
                return (null, "Uno o más productos no existen, están inactivos o no pertenecen a la empresa.");

            // 3) Transacción con aislamiento alto para evitar oversell
            await using var trx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                // 3.1) Revalida y descuenta stock
                foreach (var it in normalized)
                {
                    var p = products.First(x => x.Id == it.productId);

                    if (p.Stock < it.quantity)
                        return (null, $"Stock insuficiente para '{p.Name}'. Disponible: {p.Stock}, solicitado: {it.quantity}.");

                    p.Stock -= it.quantity;
                    p.UpdatedAt = DateTime.UtcNow;
                    // _db.Products.Update(p); // no es necesario, está en tracking
                }

                await _db.SaveChangesAsync(ct);

                // 3.2) Crea el pedido con precios "congelados" (snapshot de precio actual)
                var order = new Order
                {
                    ClienteId = clienteId,
                    EmpresaId = empresaId,
                    Fecha = DateTime.UtcNow,
                    Status = OrderStatus.Nuevo,
                    CheckoutGroupId = checkoutGroupId,
                    Items = new List<OrderItem>()
                };

                foreach (var it in normalized)
                {
                    var p = products.First(x => x.Id == it.productId);
                    var unit = p.Price;                      // double en tu modelo
                    var sub = unit * it.quantity;
                    order.Items.Add(new OrderItem
                    {
                        ProductId = p.Id,
                        Quantity  = it.quantity,
                        UnitPrice = unit,
                        Subtotal  = sub
                    });
                }

                order.Total = order.Items.Sum(i => i.Subtotal);

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(ct);

                await trx.CommitAsync(ct);
                return (order, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                await trx.RollbackAsync(ct);
                return (null, "Conflicto de concurrencia: el stock cambió durante la confirmación. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync(ct);
                return (null, $"Error al crear pedido: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea múltiples pedidos (diferentes empresas) en UNA sola transacción,
        /// compartiendo el mismo CheckoutGroupId. Si uno falla, se revierte todo.
        /// </summary>
        public async Task<(IList<Order> orders, string? error)> CreateOrdersBatchTransactionalAsync(
            int clienteId,
            IEnumerable<(int empresaId, List<(int productId, int quantity)> items)> payload,
            CancellationToken ct = default)
        {
            var list = payload?.ToList() ?? new();
            if (list.Count == 0) return (new List<Order>(), "Debes enviar al menos un pedido.");

            var groupId = Guid.NewGuid();
            var created = new List<Order>();

            await using var trx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                foreach (var (empresaId, items) in list)
                {
                    var (order, error) = await CreateOrderTransactionalAsync(
                        clienteId, empresaId, items, groupId, ct);

                    if (error != null) { await trx.RollbackAsync(ct); return (new List<Order>(), error); }
                    if (order == null)  { await trx.RollbackAsync(ct); return (new List<Order>(), "No se pudo crear uno de los pedidos."); }

                    created.Add(order);
                }

                await trx.CommitAsync(ct);
                return (created, null);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync(ct);
                return (new List<Order>(), $"Error al crear pedidos en lote: {ex.Message}");
            }
        }

        /// <summary>
        /// Cambia el estado de un pedido de una empresa. Si cancelas desde 'Nuevo', repone stock.
        /// </summary>
        public async Task<(Order? order, string? error)> ChangeStatusAsync(
            int empresaId,
            int orderId,
            OrderStatus newStatus,
            bool restockOnCancel = true,
            CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.EmpresaId == empresaId, ct);

            if (order is null) return (null, "Pedido no encontrado.");

            bool allowed = order.Status switch
            {
                OrderStatus.Nuevo     => newStatus is OrderStatus.Enviado or OrderStatus.Cancelado,
                OrderStatus.Enviado   => newStatus is OrderStatus.Entregado,
                OrderStatus.Entregado => false,
                OrderStatus.Cancelado => false,
                _ => false
            };

            if (!allowed) return (null, $"Transición inválida {order.Status} → {newStatus}.");

            await using var trx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                // Si cancelas un pedido que aún estaba 'Nuevo', reponer stock
                if (newStatus == OrderStatus.Cancelado && restockOnCancel && order.Status == OrderStatus.Nuevo)
                {
                    var ids = order.Items.Select(i => i.ProductId).ToList();
                    var products = await _db.Products
                        .Where(p => ids.Contains(p.Id) && p.EmpresaId == empresaId)
                        .ToListAsync(ct);

                    foreach (var item in order.Items)
                    {
                        var p = products.First(x => x.Id == item.ProductId);
                        p.Stock += item.Quantity;
                        p.UpdatedAt = DateTime.UtcNow;
                        // tracking habilitado
                    }
                    await _db.SaveChangesAsync(ct);
                }

                order.Status = newStatus;
                await _db.SaveChangesAsync(ct);

                await trx.CommitAsync(ct);
                return (order, null);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync(ct);
                return (null, $"Error al cambiar estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si el cliente compró (y recibió) un producto.
        /// </summary>
        public async Task<bool> ClientBoughtProductAsync(int clienteId, int productId, CancellationToken ct = default)
        {
            return await _db.Orders
                .AsNoTracking()
                .Where(o => o.ClienteId == clienteId && o.Status == OrderStatus.Entregado)
                .AnyAsync(o => o.Items.Any(i => i.ProductId == productId), ct);
        }
    }
}
