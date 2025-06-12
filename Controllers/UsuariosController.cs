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
using Microsoft.Extensions.Logging;

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IWebHostEnvironment _env;
        private const string DefaultProfilePicUrl = "https://cajutalkapi.onrender.com/uploads/default-profile.png";
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(CajuTalkContext context, IWebHostEnvironment env, IPasswordHasher<Usuario> passwordHasher, ILogger<UsuariosController> logger)
        {
            _context = context;
            _env = env;
            _passwordHasher = passwordHasher;
            _logger = logger;
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
                    FotoPerfilURL = u.FotoPerfilURL,
                    Recado = u.Recado
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
                    FotoPerfilURL = u.FotoPerfilURL,
                    Recado = u.Recado
                })
                .FirstOrDefaultAsync();

            if (usuarioDto == null)
                return NotFound();

            return Ok(usuarioDto); // Retorna o DTO encontrado
        }

        [HttpGet("buscar")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UsuarioDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> BuscarUsuarios([FromQuery] string termoBusca)
        {
            if (string.IsNullOrWhiteSpace(termoBusca))
            {
                return BadRequest("O termo de busca não pode ser vazio.");
            }

            var termoBuscaLower = termoBusca.ToLowerInvariant();

            var usuariosEncontradosDto = await _context.Usuarios
                .Where(u => 
                    (u.NomeUsuario != null && u.NomeUsuario.ToLowerInvariant().Contains(termoBuscaLower)) ||
                    (u.LoginUsuario != null && u.LoginUsuario.ToLowerInvariant().Contains(termoBuscaLower))
                )
                .Select(u => new UsuarioDto // Mapeia para o DTO
                {
                    ID = u.ID,
                    NomeUsuario = u.NomeUsuario,
                    LoginUsuario = u.LoginUsuario,
                    FotoPerfilURL = u.FotoPerfilURL,
                    Recado = u.Recado
                })
                .ToListAsync();

            return Ok(usuariosEncontradosDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] UsuarioUpdateDto updateDto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            if (!string.IsNullOrWhiteSpace(updateDto.LoginUsuario) &&
                updateDto.LoginUsuario != usuario.LoginUsuario &&
                await _context.Usuarios.AnyAsync(u => u.LoginUsuario == updateDto.LoginUsuario && u.ID != id))
            {
                return Conflict("Login já está em uso por outro usuário.");
            }

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
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, updateDto.SenhaUsuario);
            }

            usuario.Recado = updateDto.Recado;

            if (updateDto.NovaFotoPerfil != null && updateDto.NovaFotoPerfil.Length > 0)
            {
                if (!string.IsNullOrEmpty(usuario.FotoPerfilURL) && usuario.FotoPerfilURL != DefaultProfilePicUrl)
                {
                    DeleteFile(usuario.FotoPerfilURL);
                }

                try
                {
                    usuario.FotoPerfilURL = updateDto.NovaFotoPerfil;
                }
                catch (Exception ex)
                {
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
                if (!await UsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(usuario);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Sucesso
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Não encontrado
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Conflito (FK)
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Erro interno
        public async Task<IActionResult> DeletarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                _logger.LogInformation("Tentativa de deletar usuário não existente com ID {UserId}", id);
                return NotFound($"Usuário com ID {id} não encontrado.");
            }

            string fotoUrlParaDeletar = usuario.FotoPerfilURL ?? string.Empty; // Guarda a URL antes de remover

            try
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário com ID {UserId} deletado com sucesso do banco de dados.", id);

                if (!string.IsNullOrEmpty(fotoUrlParaDeletar) && fotoUrlParaDeletar != DefaultProfilePicUrl)
                {
                    DeleteFile(fotoUrlParaDeletar);
                }

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                // Log detalhado do erro de banco
                _logger.LogError(dbEx, "Erro ao deletar usuário ID {UserId} do banco de dados. Possível violação de FK.", id);
                // Retorna um erro que indica que a operação não pôde ser completada por causa de dependências
                return Conflict($"Não foi possível deletar o usuário {id}. Pode haver dados associados (mensagens, salas, etc.) que impedem a exclusão.");
            }
            catch (Exception ex) // Captura outros erros inesperados
            {
                _logger.LogError(ex, "Erro inesperado ao tentar deletar usuário ID {UserId}", id);
                return StatusCode(500, "Ocorreu um erro interno ao tentar deletar o usuário.");
            }
        }

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

            return "/uploads/" + uniqueFileName;
        }

        private void DeleteFile(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath) || string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                _logger.LogWarning("Tentativa de deletar arquivo com caminho vazio ou WebRootPath não configurado.");
                return;
            }

            string fullPath;
            try
            {
                // Tenta determinar se é URL absoluta ou caminho relativo
                if (Uri.TryCreate(relativeOrAbsolutePath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    var relativePath = uri.AbsolutePath;
                    relativePath = relativePath.TrimStart('/');
                        var uploadsFolder = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault() ?? "uploads"; // Tenta pegar a pasta raiz (ex: 'uploads')
                    var fileName = Path.GetFileName(relativePath);
                    fullPath = Path.Combine(_env.WebRootPath, uploadsFolder, fileName);

                }
                else
                {
                    var relativePath = relativeOrAbsolutePath.TrimStart('/');
                    var uploadsFolder = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault() ?? "uploads"; // Tenta pegar a pasta raiz (ex: 'uploads')
                    var fileName = Path.GetFileName(relativePath);
                    fullPath = Path.Combine(_env.WebRootPath, uploadsFolder, fileName);
                }

                _logger.LogInformation("Tentando deletar arquivo em: {FilePath}", fullPath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    _logger.LogInformation("Arquivo deletado com sucesso: {FilePath}", fullPath);
                }
                else
                {
                    _logger.LogWarning("Arquivo não encontrado para deleção: {FilePath}", fullPath);
                }
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "Erro de IO ao tentar deletar arquivo {FilePath}", relativeOrAbsolutePath);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Erro de permissão ao tentar deletar arquivo {FilePath}", relativeOrAbsolutePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao tentar deletar arquivo {FilePath}", relativeOrAbsolutePath);
            }
        }

        private async Task<bool> UsuarioExists(int id)
        {
            return await _context.Usuarios.AnyAsync(e => e.ID == id);
        }
    }
}