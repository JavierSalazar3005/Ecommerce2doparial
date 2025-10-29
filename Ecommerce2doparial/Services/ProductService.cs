using Ecommerce2doparial.Data;
using Ecommerce2doparial.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Services

{
    public class ProductService
    {
        private readonly AppDbContext _db;
        public ProductService(AppDbContext db) { _db = db; }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _db.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Product>> QueryPublicAsync(int? empresaId, decimal? min, decimal? max, string? q)
        {
            var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

            if (empresaId.HasValue) query = query.Where(p => p.EmpresaId == empresaId.Value);
            if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
            if (max.HasValue) query = query.Where(p => p.Price <= max.Value);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q) || (p.Description ?? "").Contains(q));

            return await query
                .Include(p => p.Reviews)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }
}