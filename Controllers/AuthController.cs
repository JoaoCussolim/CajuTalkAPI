using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// Removido: using System.ComponentModel.DataAnnotations; // Não mais necessário para DTOs internos
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TWTodos.Data;
using TWTodos.Models; // ****** IMPORTANTE: Incluir o namespace dos seus modelos ******
using TWTodos.Services;
using Microsoft.AspNetCore.Authorization;

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private const string DefaultProfilePicUrl = "http://localhost:5109/uploads/default-profile.png";

        public AuthController(
            CajuTalkContext context,
            IPasswordHasher<Usuario> passwordHasher,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var usuario = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.LoginUsuario == loginModel.LoginUsuario);

            if (usuario == null)
            {
                return Unauthorized(new { Message = "Login ou senha inválidos." });
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.SenhaHash,
                loginModel.SenhaUsuario
            );

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { Message = "Login ou senha inválidos." });
            }

            // Opcional: Rehash se necessário
            bool needsSave = false;
            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, loginModel.SenhaUsuario);
                needsSave = true; // Marcar que precisa salvar (além do refresh token)
            }

            // Gerar Tokens
            var accessTokenData = _tokenService.GenerateToken(usuario); // Supondo que retorna string
            var refreshToken = GenerateRefreshToken(usuario.ID);

            _context.RefreshTokens.Add(refreshToken);
            await RemoveOldRefreshTokens(usuario.ID); // Limpa tokens antigos

            // Salvar alterações (rehash e novo refresh token)
            await _context.SaveChangesAsync();

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

            // Retornar o TokenResponse do namespace TWTodos.Models
            return Ok(new TokenResponse
            {
                AccessToken = accessTokenData,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiration = accessTokenExpiration
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        // Usar o RefreshTokenRequest do namespace TWTodos.Models
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var storedRefreshToken = await _context.RefreshTokens
                .Include(rt => rt.Usuario) // Precisa do usuário para gerar novo token
                .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            // Verificar se o token existe e está ativo
            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                // Mesmo que o token exista mas esteja expirado/revogado, consideramos inválido para refresh
                return Unauthorized(new { Message = "Refresh token inválido ou expirado." });
            }

            // Gerar NOVO Access Token
            var newAccessTokenData = _tokenService.GenerateToken(storedRefreshToken.Usuario);

            // Gerar NOVO Refresh Token (Rotação)
            var newRefreshToken = GenerateRefreshToken(storedRefreshToken.UsuarioId);
            _context.RefreshTokens.Add(newRefreshToken);

            // Remover o Refresh Token ANTIGO do banco de dados
            _context.RefreshTokens.Remove(storedRefreshToken);

            // Limpar outros tokens antigos/expirados deste usuário
            await RemoveOldRefreshTokens(storedRefreshToken.UsuarioId);

            // Salvar as alterações (adição do novo token, remoção do antigo)
            await _context.SaveChangesAsync();

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

            // Retornar o TokenResponse do namespace TWTodos.Models com os NOVOS tokens
            return Ok(new TokenResponse
            {
                AccessToken = newAccessTokenData,
                RefreshToken = newRefreshToken.Token, // Retorna o NOVO refresh token
                AccessTokenExpiration = accessTokenExpiration
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        // Usar o RegisterModel do namespace TWTodos.Models
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (await _context.Usuarios.AnyAsync(u => u.LoginUsuario == model.LoginUsuario))
            {
                return Conflict(new { Message = "Login já está em uso." });
            }

            var usuario = new Usuario
            {
                NomeUsuario = model.NomeUsuario,
                LoginUsuario = model.LoginUsuario,
                FotoPerfilURL = DefaultProfilePicUrl,
                CorFundo = "255250250",
                Recado = "Olá, estou utilizando Cajutalk!"
            };

            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, model.SenhaUsuario);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var accessTokenData = _tokenService.GenerateToken(usuario);
            var refreshToken = GenerateRefreshToken(usuario.ID);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

            return Ok(new TokenResponse
            {
                AccessToken = accessTokenData,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiration = accessTokenExpiration
            });
        }

        // --- Métodos Auxiliares ---

        private RefreshToken GenerateRefreshToken(int usuarioId)
        {
            var randomNumber = new byte[64]; // Mais seguro que 32
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var tokenValue = Convert.ToBase64String(randomNumber);

            var refreshTokenValidityDays = Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

            // Usar o RefreshToken do namespace TWTodos.Models
            return new RefreshToken
            {
                Token = tokenValue, // O valor gerado
                Expires = DateTime.UtcNow.AddDays(refreshTokenValidityDays),
                Created = DateTime.UtcNow,
                UsuarioId = usuarioId
            };
        }

        private async Task RemoveOldRefreshTokens(int usuarioId, int keepActiveCount = 5)
        {
            var now = DateTime.UtcNow; // Pega o tempo atual uma vez para consistência

            var inactiveTokens = await _context.RefreshTokens
                .Where(rt => rt.UsuarioId == usuarioId &&
                            (rt.Revoked != null || rt.Expires <= now)) // Verifica se Revoked NÃO é nulo OU se Expires JÁ passou
                .ToListAsync();

            if (inactiveTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(inactiveTokens);
            }

            var activeTokensQuery = _context.RefreshTokens
                .Where(rt => rt.UsuarioId == usuarioId &&
                            rt.Revoked == null && rt.Expires > now);

            var activeTokensCount = await activeTokensQuery.CountAsync();

            if (activeTokensCount > keepActiveCount)
            {
                var tokensToRemove = await activeTokensQuery
                    .OrderBy(rt => rt.Created) // Ordena pelos mais antigos primeiro
                    .Take(activeTokensCount - keepActiveCount) // Pega a quantidade excedente
                    .ToListAsync();

                if (tokensToRemove.Any())
                {
                    _context.RefreshTokens.RemoveRange(tokensToRemove);
                }
            }
        }
    }
}