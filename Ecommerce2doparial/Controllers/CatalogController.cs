using Ecommerce2doparial.Data;
using Ecommerce2doparial.DTOs;
using Ecommerce2doparial.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly ProductService _products;
        private readonly AppDbContext _db;

        public CatalogController(ProductService products, AppDbContext db)
        {
            _products = products; _db = db;
        }

        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts(
            [FromQuery] int? empresaId, [FromQuery] decimal? precioMin, [FromQuery] decimal? precioMax, [FromQuery] string? q)
        {
            var list = await _products.QueryPublicAsync(empresaId, precioMin, precioMax, q);
            var result = list.Select(p => new ProductResponseDto
            {
                Id = p.Id,
                EmpresaId = p.EmpresaId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                AvgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : null,
                ReviewsCount = p.Reviews.Count
            });
            return Ok(result);
        }

        [HttpGet("products/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
        {
            var p = await _db.Products.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (p is null) return NotFound();
            return new ProductResponseDto
            {
                Id = p.Id,
                EmpresaId = p.EmpresaId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                AvgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : null,
                ReviewsCount = p.Reviews.Count
            };
        }
    }
}