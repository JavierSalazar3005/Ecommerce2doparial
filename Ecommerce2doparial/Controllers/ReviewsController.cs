namespace Ecommerce2doparial.Controllers;

{
    [ApiController]
    [Route("api/productos/{productId:int}/reseñas")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly OrderService _orders;
        private readonly ReviewService _reviews;

        public ReviewsController(AppDbContext db, OrderService orders, ReviewService reviews)
        {
            _db = db; _orders = orders; _reviews = reviews;
        }

        private int? CurrentUserId => User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            : null;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> List(int productId)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive))
                return NotFound("Producto no encontrado.");

            var list = await _reviews.ListByProductAsync(productId);
            var resp = list.Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                ClienteId = r.ClienteId,
                ProductId = r.ProductId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });

            return Ok(resp);
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.Cliente) + "," + nameof(Role.AdminRoot))]
        public async Task<ActionResult<ReviewResponseDto>> Create(int productId, CreateReviewDto dto)
        {
            var uid = CurrentUserId!.Value;

            var bought = await _orders.ClientBoughtProductAsync(uid, productId);
            if (!bought)
                return BadRequest("Solo puedes reseñar productos que hayas recibido (pedido ENTREGADO).");

            var (review, error) = await _reviews.CreateAsync(uid, productId, dto.Rating, dto.Comment);
            if (error is not null) return BadRequest(error);
            if (review is null) return Problem("No se pudo crear la reseña.");

            return CreatedAtAction(nameof(List), new { productId }, new ReviewResponseDto
            {
                Id = review.Id,
                ClienteId = review.ClienteId,
                ProductId = review.ProductId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            });
        }
    }
}