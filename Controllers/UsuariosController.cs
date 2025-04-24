using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;
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
        public async Task<IActionResult> CriarUsuario([FromBody] Usuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.NomeUsuario) || string.IsNullOrWhiteSpace(usuario.LoginUsuario) || string.IsNullOrWhiteSpace(usuario.SenhaHash))
            {
                return BadRequest("Nome, Login e Senha são obrigatórios.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.LoginUsuario == usuario.LoginUsuario))
            {
                return Conflict("Login já está em uso.");
            }

            usuario.FotoPerfilURL = DefaultProfilePicUrl;

            // *** Hash the password before saving ***
            // The first parameter (usuario) is optional here but required by the interface contract.
            // It's used by Identity for potential user-specific hashing upgrades, but not strictly needed for basic hashing.
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, usuario.SenhaHash);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // IMPORTANT: Do NOT return the hashed password in the response.
            // Create a DTO (Data Transfer Object) for responses.
            // For simplicity now, we return the object but clear the sensitive field.
            usuario.SenhaHash = null; // Clear password before returning

            return CreatedAtAction(nameof(ObterPorId), new { id = usuario.ID }, usuario);
        }

        // GET /usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> ObterTodos()
        {
            // Consider projecting to a DTO to avoid exposing sensitive data like passwords
            return await _context.Usuarios.ToListAsync();
        }

        // GET /usuarios/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> ObterPorId(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound();

            // Consider projecting to a DTO
            return usuario;
        }

        // PUT /usuarios/{id}
        // Updates user information, optionally including a new profile picture
        [HttpPut("{id}")]
        // We use [FromForm] because we might receive a file (multipart/form-data)
        // Create a specific DTO for updates is recommended practice
        public async Task<IActionResult> AtualizarUsuario(int id, [FromForm] UsuarioUpdateModel updateModel)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            // Check for login conflict only if the login is being changed
            if (!string.IsNullOrWhiteSpace(updateModel.LoginUsuario) &&
                updateModel.LoginUsuario != usuario.LoginUsuario &&
                await _context.Usuarios.AnyAsync(u => u.LoginUsuario == updateModel.LoginUsuario && u.ID != id))
            {
                return Conflict("Login já está em uso por outro usuário.");
            }

            // Update properties if provided in the model
            if (!string.IsNullOrWhiteSpace(updateModel.NomeUsuario))
            {
                usuario.NomeUsuario = updateModel.NomeUsuario;
            }
            if (!string.IsNullOrWhiteSpace(updateModel.LoginUsuario))
            {
                usuario.LoginUsuario = updateModel.LoginUsuario;
            }
            if (!string.IsNullOrWhiteSpace(updateModel.SenhaUsuario))
            {
                // *** Hash the password IF it's being updated ***
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, updateModel.SenhaUsuario);
            }

            // Handle optional photo upload
            if (updateModel.NovaFotoPerfil != null && updateModel.NovaFotoPerfil.Length > 0)
            {
                // Delete old photo if it's not the default one
                if (!string.IsNullOrEmpty(usuario.FotoPerfilURL) && usuario.FotoPerfilURL != DefaultProfilePicUrl)
                {
                    DeleteFile(usuario.FotoPerfilURL);
                }

                // Save new photo and update URL
                try
                {
                    usuario.FotoPerfilURL = await SaveFileAsync(updateModel.NovaFotoPerfil);
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

            // Return NoContent or the updated user (projected to DTO)
            return NoContent(); // Standard REST response for successful PUT
            // Or return Ok(usuario); // If you want to return the updated object
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

        // *** Define a specific model for the update payload ***
        // This is better practice than reusing the main Usuario model directly
        public class UsuarioUpdateModel
        {
            public string? NomeUsuario { get; set; } // Nullable allows partial updates
            public string? LoginUsuario { get; set; }
            public string? SenhaUsuario { get; set; } // Handle password updates carefully
            public IFormFile? NovaFotoPerfil { get; set; } // Optional new photo
             // Don't include ID here, it comes from the route
             // Don't include FotoPerfilURL here, it's handled internally if NovaFotoPerfil is provided
        }
    }
}