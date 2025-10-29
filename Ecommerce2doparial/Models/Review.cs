using System.ComponentModel.DataAnnotations;

namespace Ecommerce2doparial.Models;


public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int ClienteId { get; set; }
    public User Cliente { get; set; } = null!;

    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}