using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;
using TWTodos.DTOs;
using System.IO; // Required for Path operations
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using System; // Required for Guid
using Microsoft.AspNetCore.Identity; // Required for hashing passwords
using System.Threading.Tasks; // Required for async operations
using System.Collections.Generic; // Required for IEnumerable
using System.Linq; // Required for LINQ extension methods like AnyAsync

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IWebHostEnvironment _env;
        private const string DefaultProfilePicUrl = "http://localhost:5109/uploads/default-profile.png";
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(CajuTalkContext context, IWebHostEnvironment env, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _env = env;
            _passwordHasher = passwordHasher;
        }

        // POST /usuarios
        // Creates a new user with a default profile picture
        [HttpPost]
        public async Task<IActionResult> CriarUsuario([FromBody] UsuarioCreateDto usuarioDto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.LoginUsuario == usuarioDto.LoginUsuario))
            {
                return Conflict("Login já está em uso.");
            }

            // *** Mapeamento Manual do DTO para a Entidade ***
            var usuario = new Usuario
            {
                NomeUsuario = usuarioDto.NomeUsuario,
                LoginUsuario = usuarioDto.LoginUsuario,
                FotoPerfilURL = DefaultProfilePicUrl // Pega o padrão
                // SenhaHash será definida abaixo
            };

            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, usuarioDto.SenhaUsuario);
            usuario.CorFundo = "255250250";

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            // *** Mapeamento da Entidade para o DTO de Resposta ***
            var usuarioResultDto = new UsuarioDto
            {
                ID = usuario.ID,
                NomeUsuario = usuario.NomeUsuario,
                LoginUsuario = usuario.LoginUsuario,
                FotoPerfilURL = usuario.FotoPerfilURL,
                CorFundo = usuario.CorFundo
            };

            // Retorna o DTO de resposta
            return CreatedAtAction(nameof(ObterPorId), new { id = usuarioResultDto.ID }, usuarioResultDto);
        }

        // GET /usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> ObterTodos()
        {
            var usuariosDto = await _context.Usuarios
                .Select(u => new UsuarioDto // Mapeia para o DTO
                {
                    ID = u.ID,
                    NomeUsuario = u.NomeUsuario,
                    LoginUsuario = u.LoginUsuario,
                    FotoPerfilURL = u.FotoPerfilURL
                })
                .ToListAsync();

            return Ok(usuariosDto); // Retorna a lista de DTOs
        }

        // GET /usuarios/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> ObterPorId(int id)
        {
            var usuarioDto = await _context.Usuarios
                .Where(u => u.ID == id)
                .Select(u => new UsuarioDto // Mapeia para o DTO
                {
                    ID = u.ID,
                    NomeUsuario = u.NomeUsuario,
                    LoginUsuario = u.LoginUsuario,
                    FotoPerfilURL = u.FotoPerfilURL
                })
                .FirstOrDefaultAsync();

            if (usuarioDto == null)
                return NotFound();

            return Ok(usuarioDto); // Retorna o DTO encontrado
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(int id, [FromForm] UsuarioUpdateDto updateDto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            // Check for login conflict only if the login is being changed
            if (!string.IsNullOrWhiteSpace(updateDto.LoginUsuario) &&
                updateDto.LoginUsuario != usuario.LoginUsuario &&
                await _context.Usuarios.AnyAsync(u => u.LoginUsuario == updateDto.LoginUsuario && u.ID != id))
            {
                return Conflict("Login já está em uso por outro usuário.");
            }

            // Update properties if provided in the model
            if (!string.IsNullOrWhiteSpace(updateDto.NomeUsuario))
            {
                usuario.NomeUsuario = updateDto.NomeUsuario;
            }
            if (!string.IsNullOrWhiteSpace(updateDto.LoginUsuario))
            {
                usuario.LoginUsuario = updateDto.LoginUsuario;
            }
            if (!string.IsNullOrWhiteSpace(updateDto.SenhaUsuario))
            {
                // *** Hash the password IF it's being updated ***
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, updateDto.SenhaUsuario);
            }

            // Handle optional photo upload
            if (updateDto.NovaFotoPerfil != null && updateDto.NovaFotoPerfil.Length > 0)
            {
                // Delete old photo if it's not the default one
                if (!string.IsNullOrEmpty(usuario.FotoPerfilURL) && usuario.FotoPerfilURL != DefaultProfilePicUrl)
                {
                    DeleteFile(usuario.FotoPerfilURL);
                }

                // Save new photo and update URL
                try
                {
                    usuario.FotoPerfilURL = await SaveFileAsync(updateDto.NovaFotoPerfil);
                }
                catch (Exception ex)
                {
                    // Log the exception
                    return StatusCode(500, $"Erro interno ao salvar a imagem: {ex.Message}");
                }
            }

            try
            {
                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle potential concurrency issues if needed
                if (!await UsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(usuario); // Standard REST response for successful PUT
        }


        // Helper method to save uploaded file
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                throw new InvalidOperationException("WebRootPath não está configurado.");
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path accessible by the web server
            return "/uploads/" + uniqueFileName;
        }

        // Helper method to delete a file
        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                return;
            }

             // Remove leading slash if present to correctly combine with WebRootPath
             var fileName = Path.GetFileName(relativePath); // Or manipulate the path string carefully
             var fullPath = Path.Combine(_env.WebRootPath, "uploads", fileName); // Assumes files are always in /uploads/

            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                 // Log the error (e.g., using ILogger)
                Console.WriteLine($"Erro ao deletar arquivo '{fullPath}': {ex.Message}");
                 // Decide if this should halt the operation or just be logged
            }
        }


        // Helper method to check if user exists
        private async Task<bool> UsuarioExists(int id)
        {
            return await _context.Usuarios.AnyAsync(e => e.ID == id);
        }
    }
}