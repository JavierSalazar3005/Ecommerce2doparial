using Ecommerce2doparial.Data;
using Ecommerce2doparial.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Services

{
    public class ReviewService
    {
        private readonly AppDbContext _db;
        public ReviewService(AppDbContext db) => _db = db;

        public async Task<(Review? review, string? error)> CreateAsync(
            int clienteId, int productId, int rating, string? comment)
        {
            // Validar producto activo
            var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
            if (!exists) return (null, "Producto no encontrado o inactivo.");

            // Unicidad (ProductoId, ClienteId)
            var already = await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.ClienteId == clienteId);
            if (already) return (null, "Ya registraste una reseña para este producto.");

            var r = new Review
            {
                ProductId = productId,
                ClienteId = clienteId,
                Rating = rating,
                Comment = comment
            };

            _db.Reviews.Add(r);
            await _db.SaveChangesAsync();
            return (r, null);
        }

        public async Task<List<Review>> ListByProductAsync(int productId)
        {
            return await _db.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}