namespace Ecommerce2doparial.Controllers;

{
    [ApiController]
    [Route("api/empresa/products")]
    [Authorize(Roles = nameof(Role.Empresa) + "," + nameof(Role.AdminRoot))]
    public class EmpresaProductsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EmpresaProductsController(AppDbContext db) { _db = db; }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin => User.IsInRole(Role.AdminRoot.ToString());

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> MyProducts()
        {
            var empresaId = CurrentUserId;
            var query = _db.Products.AsQueryable();
            if (!IsAdmin) query = query.Where(p => p.EmpresaId == empresaId);

            var list = await query.Include(p => p.Reviews).OrderByDescending(p => p.Id).ToListAsync();
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

        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> Create(ProductCreateDto dto)
        {
            var empresaId = CurrentUserId;

            var p = new Product
            {
                EmpresaId = empresaId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock
            };
            _db.Products.Add(p);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = p.Id }, new ProductResponseDto
            {
                Id = p.Id, EmpresaId = p.EmpresaId, Name = p.Name, Description = p.Description,
                Price = p.Price, Stock = p.Stock, IsActive = p.IsActive
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetById(int id)
        {
            var p = await _db.Products.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (!IsAdmin && p.EmpresaId != CurrentUserId) return Forbid();

            return new ProductResponseDto
            {
                Id = p.Id, EmpresaId = p.EmpresaId, Name = p.Name, Description = p.Description,
                Price = p.Price, Stock = p.Stock, IsActive = p.IsActive,
                AvgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : null,
                ReviewsCount = p.Reviews.Count
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ProductUpdateDto dto)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (!IsAdmin && p.EmpresaId != CurrentUserId) return Forbid();

            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Price = dto.Price;
            p.Stock = dto.Stock;
            if (dto.IsActive.HasValue) p.IsActive = dto.IsActive.Value;
            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (!IsAdmin && p.EmpresaId != CurrentUserId) return Forbid();

            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/activar")]
        public async Task<IActionResult> Activar(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (!IsAdmin && p.EmpresaId != CurrentUserId) return Forbid();

            p.IsActive = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (!IsAdmin && p.EmpresaId != CurrentUserId) return Forbid();

            p.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}