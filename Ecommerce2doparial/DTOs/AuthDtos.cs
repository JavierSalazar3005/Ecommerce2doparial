namespace Ecommerce2doparial.DTOs;

{
    public class RegisterDto
    {
        [Required, EmailAddress] public string Email { get; set; } = null!;
        [Required, MinLength(6)] public string Password { get; set; } = null!;
        [Required] public Role Role { get; set; } // Empresa o Cliente
        public string? CompanyName { get; set; }   // Requerido si Empresa (validar en controller)
    }

    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public Role Role { get; set; }
        public string? CompanyName { get; set; }
        public string Token { get; set; } = null!;
    }
}