using System.ComponentModel.DataAnnotations;

namespace Ecommerce2doparial.Models;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public Role Role { get; set; }

    // Para Empresas: nombre comercial. Para Cliente, puede ser null.
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}