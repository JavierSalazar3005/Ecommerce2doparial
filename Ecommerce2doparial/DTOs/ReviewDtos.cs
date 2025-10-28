namespace Ecommerce2doparial.DTOs;

{
    public class CreateReviewDto
    {
        [Range(1,5)] public int Rating { get; set; }
        [MaxLength(500)] public string? Comment { get; set; }
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}