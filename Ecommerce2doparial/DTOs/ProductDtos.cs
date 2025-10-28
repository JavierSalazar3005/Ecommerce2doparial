namespace Ecommerce2doparial.DTOs;

{
    public class ProductCreateDto
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = null!;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Range(0.01, 999999999)]
        public decimal Price { get; set; }
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }
    }

    public class ProductUpdateDto : ProductCreateDto
    {
        public bool? IsActive { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public double? AvgRating { get; set; }
        public int ReviewsCount { get; set; }
    }
}