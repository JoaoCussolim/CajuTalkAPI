using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IWebHostEnvironment _env;

        public UsuariosController(CajuTalkContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Rota Post para criar novos usuários
        [HttpPost]
        public async Task<IActionResult> CriarUsuario([FromBody] Usuario usuario)
        {
            if (await _context.Usuarios.AnyAsync(u => u.LoginUsuario == usuario.LoginUsuario))
            {
                return Conflict("Email ou login j� est� em uso.");
            }

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = usuario.ID }, usuario);
        }

        // Rota Get para obter todos os usuários
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> ObterTodos()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // Rota Get para obter os dados do usuário selecionados
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> ObterPorId(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound();

            return usuario;
        }

        // Rota Post para trocar a foto de perfil de um usuário
        [HttpPost("upload-foto")]
        public async Task<IActionResult> UploadFoto([FromForm] IFormFile imagem, [FromForm] int usuarioID)
        {
            if (imagem == null || imagem.Length == 0)
                return BadRequest("Imagem inv�lida.");

            var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(imagem.FileName);
            var caminhoUploads = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(caminhoUploads))
                Directory.CreateDirectory(caminhoUploads);

            var caminhoCompleto = Path.Combine(caminhoUploads, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await imagem.CopyToAsync(stream);
            }

            var urlRelativa = "/uploads/" + nomeArquivo;

            var usuario = await _context.Usuarios.FindAsync(usuarioID);
            if (usuario == null)
                return NotFound("Usu�rio n�o encontrado.");

            usuario.FotoPerfilURL = urlRelativa;
            await _context.SaveChangesAsync();

            return Ok(new { url = urlRelativa });
        }
    }
}
