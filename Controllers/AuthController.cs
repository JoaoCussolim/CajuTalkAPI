using Microsoft.AspNetCore.Mvc;              // For ApiController, ControllerBase, Route, IActionResult, HttpPost, FromBody
using Microsoft.AspNetCore.Identity;         // For IPasswordHasher, PasswordVerificationResult
using Microsoft.EntityFrameworkCore;         // For FirstOrDefaultAsync
using System.ComponentModel.DataAnnotations; // For [Required]
using System.Threading.Tasks;              // For Task<>
using TWTodos.Data;                          // For CajuTalkContext (Ensure this is the correct namespace)
using TWTodos.Models;                        // For Usuario (Ensure this is the correct namespace)
using TWTodos.Services;                      // For ITokenService (You already have this one)

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthController(CajuTalkContext context, IPasswordHasher<Usuario> passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public class LoginModel
        {
            [Required]
            public string LoginUsuario { get; set; }
            [Required]
            public string SenhaUsuario { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var usuario = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.LoginUsuario == loginModel.LoginUsuario);

            if (usuario == null)
            {
                return Unauthorized("Login ou senha inválidos.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.SenhaHash,
                loginModel.SenhaUsuario
            );

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Login ou senha inválidos.");
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, loginModel.SenhaUsuario);
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateToken(usuario);
            return Ok(new { Token = token });
        }
    }
}