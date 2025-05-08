using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;
using TWTodos.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Para IFormFile e StatusCodes
using System.IO; // Para Path e FileStream
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class MensagensController : ControllerBase
    {
        private readonly CajuTalkContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Para caminhos de arquivos

        public MensagensController(CajuTalkContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
            {
                return true;
            }
            return false;
        }

        [HttpPost]
        // Mude para [FromForm] para aceitar multipart/form-data
        public async Task<IActionResult> EnviarMensagem([FromForm] MensagemCreateDto mensagemCreateDto)
        {
            if (!TryGetUserId(out int remetenteId))
            {
                return Unauthorized("Remetente não identificado.");
            }

            var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == mensagemCreateDto.IDSala);
            if (!salaExiste)
            {
                return NotFound($"Sala com ID {mensagemCreateDto.IDSala} não encontrada.");
            }

            bool podeEnviar = await _context.UsuarioSala
                .AnyAsync(us => us.ID_Sala == mensagemCreateDto.IDSala &&
                                us.ID_Usuario == remetenteId &&
                                !us.UsuarioBanido);
            if (!podeEnviar)
            {
                return Forbid("Usuário não tem permissão para enviar mensagens nesta sala ou está banido.");
            }

            string? conteudoMensagem = mensagemCreateDto.Conteudo;
            string tipoMensagem = mensagemCreateDto.TipoMensagem ?? "texto"; // Garante que não seja nulo

            // Lógica de Upload de Arquivo
            string? arquivoUrl = null;
            if (mensagemCreateDto.Arquivo != null && mensagemCreateDto.Arquivo.Length > 0)
            {
                // Validar tipo de arquivo (opcional, mas recomendado)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".mp4", ".mov", ".mp3", ".wav" };
                var ext = Path.GetExtension(mensagemCreateDto.Arquivo.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return BadRequest("Tipo de arquivo inválido.");
                }

                long maxFileSize = 10 * 1024 * 1024; // 10 MB
                if (mensagemCreateDto.Arquivo.Length > maxFileSize)
                {
                    return BadRequest($"O arquivo excede o tamanho máximo permitido de {maxFileSize / (1024*1024)}MB.");
                }


                // Determinar TipoMensagem com base na extensão do arquivo, se não especificado
                if (string.IsNullOrWhiteSpace(mensagemCreateDto.TipoMensagem) || mensagemCreateDto.TipoMensagem.Equals("texto", StringComparison.OrdinalIgnoreCase))
                {
                    if (new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext)) tipoMensagem = "imagem";
                    else if (new[] { ".mp4", ".mov", ".avi" }.Contains(ext)) tipoMensagem = "video";
                    else if (new[] { ".mp3", ".wav", ".ogg" }.Contains(ext)) tipoMensagem = "audio";
                    else tipoMensagem = "arquivo";
                }


                // Caminho para a pasta 'uploads' dentro de 'wwwroot'
                string uploadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath); // Cria a pasta se não existir
                }

                // Gerar um nome de arquivo único para evitar sobrescrever arquivos com o mesmo nome
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mensagemCreateDto.Arquivo.FileName);
                string filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mensagemCreateDto.Arquivo.CopyToAsync(stream);
                    }
                    // A URL será relativa à raiz do site, apontando para a pasta 'uploads'
                    // Ex: /uploads/guid_nomedoarquivo.ext
                    arquivoUrl = $"/uploads/{uniqueFileName}";
                    conteudoMensagem = arquivoUrl; // Para mensagens de arquivo, o conteúdo é a URL
                }
                catch (Exception ex)
                {
                    // Log o erro (ex.ToString())
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao salvar o arquivo.");
                }
            }
            else if (string.IsNullOrWhiteSpace(mensagemCreateDto.Conteudo))
            {
                // Se não há arquivo E não há conteúdo de texto, a mensagem é inválida.
                return BadRequest("A mensagem deve conter texto ou um arquivo.");
            }


            var mensagem = new Mensagem
            {
                ID_Sala = mensagemCreateDto.IDSala,
                ID_Usuario = remetenteId,
                Conteudo = conteudoMensagem ?? string.Empty, // Se for só arquivo, arquivoUrl. Se só texto, o texto.
                TipoMensagem = tipoMensagem, // "texto", "imagem", "video", "audio", "arquivo"
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            var remetente = await _context.Usuarios
                                   .Where(u => u.ID == remetenteId)
                                   .Select(u => new { u.LoginUsuario, u.FotoPerfilURL })
                                   .FirstOrDefaultAsync();

            if (remetente == null)
            {
                return StatusCode(500, "Erro ao buscar dados do remetente após salvar a mensagem.");
            }

            var mensagemDto = new MensagemDto
            {
                Id = mensagem.ID,
                SalaId = mensagem.ID_Sala,
                Conteudo = mensagem.Conteudo, // Será a URL do arquivo se for um arquivo
                DataEnvio = mensagem.DataEnvio,
                TipoMensagem = mensagem.TipoMensagem,
                UsuarioId = mensagem.ID_Usuario,
                LoginUsuario = remetente.LoginUsuario,
                FotoPerfilURL = remetente.FotoPerfilURL
            };

            return Ok(mensagemDto);
        }

        // Rota get que obtem as mensagens de uma sala usando DTO
        [HttpGet("sala/{idSala}")]
        public async Task<ActionResult<IEnumerable<MensagemDto>>> ObterMensagensPorSala(int idSala)
        {
            if (!TryGetUserId(out int requestingUserId))
            {
                return Unauthorized("Usuário não identificado.");
            }

            var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == idSala);
            if (!salaExiste)
            {
                return NotFound($"Sala com ID {idSala} não encontrada.");
            }

            bool podeLer = await _context.UsuarioSala
                .AnyAsync(us => us.ID_Sala == idSala &&
                                us.ID_Usuario == requestingUserId &&
                                !us.UsuarioBanido);
            if (!podeLer)
            {
                return Forbid("Usuário não tem permissão para ler mensagens nesta sala.");
            }

            var mensagensDto = await _context.Mensagens
                .Where(m => m.ID_Sala == idSala)
                .OrderBy(m => m.DataEnvio)
                .Join(
                    _context.Usuarios,
                    msg => msg.ID_Usuario,
                    usr => usr.ID,
                    (msg, usr) => new MensagemDto
                    {
                        Id = msg.ID,
                        SalaId = msg.ID_Sala,
                        Conteudo = msg.Conteudo, // Este será o link para o arquivo, se aplicável
                        DataEnvio = msg.DataEnvio,
                        TipoMensagem = msg.TipoMensagem,
                        UsuarioId = msg.ID_Usuario,
                        LoginUsuario = usr.LoginUsuario,
                        FotoPerfilURL = usr.FotoPerfilURL
                    })
                .ToListAsync();

            return Ok(mensagensDto);
        }
    }
}