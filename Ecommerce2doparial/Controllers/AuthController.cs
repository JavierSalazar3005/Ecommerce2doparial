using Ecommerce2doparial.Data;
using Ecommerce2doparial.DTOs;
using Ecommerce2doparial.Models;
using Ecommerce2doparial.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce2doparial.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _token;

        public AuthController(AppDbContext db, TokenService token)
        {
            _db = db; _token = token;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email ya registrado.");

            if (dto.Role == Role.Empresa && string.IsNullOrWhiteSpace(dto.CompanyName))
                return BadRequest("CompanyName es requerido para rol Empresa.");

            var user = new User
            {
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                CompanyName = dto.Role == Role.Empresa ? dto.CompanyName : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _token.CreateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                CompanyName = user.CompanyName,
                Token = token
            };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null) return Unauthorized("Credenciales inválidas.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Credenciales inválidas.");

            var token = _token.CreateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                CompanyName = user.CompanyName,
                Token = token
            };
        }
    }
}