using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;
using TWTodos.DTOs; // Nossos DTOs
using System.Security.Claims; // Para ClaimTypes
using Microsoft.AspNetCore.Authorization; // Para [Authorize]
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic; // Para IEnumerable
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using Microsoft.Extensions.Logging; // Required for ILogger
using System;
using System.IO; // Required for Path, File operations
using Microsoft.AspNetCore.Http; // Required for StatusCodes and IFormFile

namespace TWTodos.Controllers
{
[ApiController]
[Route("[controller]")] // Rota base /mensagens
[Authorize] // Geralmente, todas as ações com mensagens exigem login
public class MensagensController : ControllerBase
{
    private readonly CajuTalkContext _context;
    private readonly IWebHostEnvironment _env; // Para acesso ao wwwroot
    private readonly ILogger<MensagensController> _logger; // Para logs

    // Injete IWebHostEnvironment e ILogger
    public MensagensController(CajuTalkContext context, IWebHostEnvironment env, ILogger<MensagensController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // --- Helper para obter User ID do Token ---
    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // Padrão do Identity
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
        {
            return true;
        }
        _logger.LogWarning("Não foi possível obter o ID do usuário autenticado a partir do token.");
        return false;
    }

    // POST /mensagens - Agora aceita Form Data para lidar com arquivos
    [HttpPost]
    // [RequestSizeLimit(100_000_000)] // Opcional: Definir limite de tamanho (ex: 100MB) aqui ou globalmente
    public async Task<IActionResult> EnviarMensagem([FromForm] MensagemCreateDto mensagemCreateDto) // <-- MUDANÇA: [FromForm]
    {
        // 1. Obter ID do remetente
        if (!TryGetUserId(out int remetenteId))
        {
            return Unauthorized("Remetente não identificado.");
        }

        // 2. Validar Input Básico
        bool isFileType = mensagemCreateDto.TipoMensagem != TipoMensagemEnum.Texto;
        if (!isFileType) // É Mensagem de Texto
        {
            if (string.IsNullOrWhiteSpace(mensagemCreateDto.Conteudo))
            {
                return BadRequest("O campo 'Conteudo' é obrigatório para mensagens de texto.");
            }
            if (mensagemCreateDto.MediaFile != null)
            {
                return BadRequest("Não envie arquivos ('MediaFile') para mensagens de texto.");
            }
        }
        else // É Mensagem de Arquivo (Audio, Video, Imagem, Arquivo)
        {
            if (mensagemCreateDto.MediaFile == null || mensagemCreateDto.MediaFile.Length == 0)
            {
                return BadRequest($"O campo 'MediaFile' é obrigatório para mensagens do tipo {mensagemCreateDto.TipoMensagem}.");
            }

            // **VALIDAÇÃO DE TAMANHO (ESSENCIAL!)**
            const long maxFileSize = 50 * 1024 * 1024; // Ex: 50 MB - AJUSTE CONFORME NECESSÁRIO
            if (mensagemCreateDto.MediaFile.Length > maxFileSize)
            {
                    _logger.LogWarning("Tentativa de upload de arquivo ({FileName}, {FileSize} bytes) excedeu o limite por usuário {UserId}",
                    mensagemCreateDto.MediaFile.FileName, mensagemCreateDto.MediaFile.Length, remetenteId);
                return BadRequest($"O arquivo excede o tamanho máximo permitido de {maxFileSize / 1024 / 1024} MB.");
            }

            // **VALIDAÇÃO DE TIPO (Opcional, mas recomendado para tipos específicos)**
            // if (mensagemCreateDto.TipoMensagem == TipoMensagemEnum.Imagem && !IsValidMediaType(mensagemCreateDto.MediaFile, TipoMensagemEnum.Imagem))
            //     return BadRequest("Arquivo inválido para mensagem do tipo Imagem.");
            // if (mensagemCreateDto.TipoMensagem == TipoMensagemEnum.Audio && !IsValidMediaType(mensagemCreateDto.MediaFile, TipoMensagemEnum.Audio))
            //     return BadRequest("Arquivo inválido para mensagem do tipo Audio.");
            // if (mensagemCreateDto.TipoMensagem == TipoMensagemEnum.Video && !IsValidMediaType(mensagemCreateDto.MediaFile, TipoMensagemEnum.Video))
            //     return BadRequest("Arquivo inválido para mensagem do tipo Video.");

            // **SEGURANÇA: Blacklist/Allowlist para TipoMensagemEnum.Arquivo (MUITO IMPORTANTE!)**
                if (mensagemCreateDto.TipoMensagem == TipoMensagemEnum.Arquivo)
                {
                    var dangerousExtensions = new[] { ".exe", ".dll", ".bat", ".sh", ".js", ".html", ".htm", ".msi", ".vbs" };
                    var fileExt = Path.GetExtension(mensagemCreateDto.MediaFile.FileName)?.ToLowerInvariant();
                    if (dangerousExtensions.Contains(fileExt)) {
                        _logger.LogWarning("Tentativa de upload de arquivo potencialmente perigoso bloqueado: {FileName} por usuário {UserId}", mensagemCreateDto.MediaFile.FileName, remetenteId);
                        return BadRequest("Tipo de arquivo não permitido por motivos de segurança.");
                    }
                }
        }


        // 3. Verificar se a sala existe e se o usuário pode enviar
        var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == mensagemCreateDto.SalaId);
        if (!salaExiste)
        {
            return NotFound($"Sala com ID {mensagemCreateDto.SalaId} não encontrada.");
        }

        bool podeEnviar = await _context.UsuarioSala
            .AnyAsync(us => us.ID_Sala == mensagemCreateDto.SalaId &&
                            us.ID_Usuario == remetenteId &&
                            !us.UsuarioBanido);
        if (!podeEnviar)
        {
                _logger.LogWarning("Usuário {UserId} tentou enviar mensagem para sala {SalaId} sem permissão (banido ou não membro).", remetenteId, mensagemCreateDto.SalaId);
            return Forbid("Você não tem permissão para enviar mensagens nesta sala.");
        }

        // 4. Processar Upload e Criar Entidade
        string? mediaUrl = null;
        string? originalFileName = null;

        if (isFileType && mensagemCreateDto.MediaFile != null)
        {
            try
            {
                originalFileName = SanitizeFileName(mensagemCreateDto.MediaFile.FileName); // Limpa o nome original
                mediaUrl = await SaveMediaFileAsync(mensagemCreateDto.MediaFile); // Salva o arquivo
            }
            catch (IOException ioEx) {
                    _logger.LogError(ioEx, "Erro de IO ao salvar arquivo {FileName} para usuário {UserId} na sala {SalaId}", mensagemCreateDto.MediaFile.FileName, remetenteId, mensagemCreateDto.SalaId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao acessar o armazenamento de arquivos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao salvar arquivo {FileName} para usuário {UserId} na sala {SalaId}", mensagemCreateDto.MediaFile.FileName, remetenteId, mensagemCreateDto.SalaId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro interno ao processar o arquivo.");
            }
        }

        // Mapear para a entidade Mensagem
        var mensagem = new Mensagem
        {
            ID_Sala = mensagemCreateDto.SalaId,
            ID_Usuario = remetenteId,
            Conteudo = !isFileType ? mensagemCreateDto.Conteudo : mediaUrl, // Texto ou URL do arquivo
            TipoMensagem = mensagemCreateDto.TipoMensagem,
            DataEnvio = DateTime.UtcNow
        };

        // 5. Salvar no Banco
        _context.Mensagens.Add(mensagem);
        await _context.SaveChangesAsync();

        // 6. Preparar DTO de resposta
            var remetente = await _context.Usuarios
                                    .Where(u => u.ID == remetenteId)
                                    .Select(u => new { u.LoginUsuario, u.FotoPerfilURL })
                                    .FirstOrDefaultAsync();

        // É improvável, mas pode acontecer se o usuário for deletado entre o login e aqui
        if (remetente == null) {
                _logger.LogError("Remetente (Usuário ID: {UserId}) não encontrado no banco após salvar mensagem {MessageId}", remetenteId, mensagem.ID);
                // Retornar a mensagem mesmo assim pode ser ok, mas sem dados do remetente. Ou retornar erro.
                // Decisão: Retornar erro para indicar inconsistência.
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao buscar dados do remetente após salvar a mensagem.");
        }

        var mensagemDto = new MensagemDto
        {
            Id = mensagem.ID,
            SalaId = mensagem.ID_Sala,
            Conteudo = mensagem.Conteudo,
            DataEnvio = mensagem.DataEnvio,
            TipoMensagem = mensagem.TipoMensagem,
            UsuarioId = mensagem.ID_Usuario,
            LoginUsuario = remetente.LoginUsuario,
            FotoPerfilURL = remetente.FotoPerfilURL,
        };

        // 7. Retornar
        // Poderia usar CreatedAtAction se houvesse um GET /mensagens/{id}
        return Ok(mensagemDto);
    }

    // GET /mensagens/sala/{SalaId} - Busca mensagens com dados do remetente
    [HttpGet("sala/{SalaId}")]
    public async Task<ActionResult<IEnumerable<MensagemDto>>> ObterMensagensPorSala(int SalaId)
    {
            // 1. Obter ID do usuário requisitante
        if (!TryGetUserId(out int requestingUserId))
        {
            return Unauthorized("Usuário não identificado.");
        }

        // 2. Verificar se a sala existe
        var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == SalaId);
        if (!salaExiste)
        {
            return NotFound($"Sala com ID {SalaId} não encontrada.");
        }

        // 3. Verificar se o usuário pertence à sala para poder ler as mensagens
            bool podeLer = await _context.UsuarioSala
            .AnyAsync(us => us.ID_Sala == SalaId &&
                            us.ID_Usuario == requestingUserId &&
                            !us.UsuarioBanido); // Não pode estar banido
        if (!podeLer)
        {
            _logger.LogWarning("Usuário {UserId} tentou ler mensagens da sala {SalaId} sem permissão.", requestingUserId, SalaId);
            return Forbid("Você não tem permissão para ler mensagens nesta sala.");
        }

        // 4. Buscar mensagens e informações do remetente, incluindo NomeArquivoOriginal
        var mensagensDto = await _context.Mensagens
            .Where(m => m.ID_Sala == SalaId)
            .OrderBy(m => m.DataEnvio) // Ordena pela data de envio
            .Join( // Junta com Usuarios para pegar nome/foto do remetente
                _context.Usuarios,
                msg => msg.ID_Usuario, // Chave na Mensagem
                usr => usr.ID,         // Chave no Usuario
                (msg, usr) => new MensagemDto // Projeta o resultado no DTO
                {
                    Id = msg.ID,
                    SalaId = msg.ID_Sala,
                    Conteudo = msg.Conteudo, // Contém texto ou URL
                    DataEnvio = msg.DataEnvio,
                    TipoMensagem = msg.TipoMensagem,
                    UsuarioId = msg.ID_Usuario,
                    LoginUsuario = usr.LoginUsuario,
                    FotoPerfilURL = usr.FotoPerfilURL,
                })
            .ToListAsync(); // Executa a query

        return Ok(mensagensDto);
    }

    // --- Métodos Helper ---

    // Salva o arquivo e retorna a URL relativa
    private async Task<string> SaveMediaFileAsync(IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(_env.WebRootPath))
        {
            _logger.LogError("IWebHostEnvironment.WebRootPath não está configurado.");
            throw new InvalidOperationException("WebRootPath não está configurado para salvar arquivos.");
        }

        // Define o caminho relativo e absoluto da pasta de uploads
        var mediaFolderRelative = Path.Combine("uploads", "media"); // Ex: "uploads/media"
        var uploadsFolderPathAbsolute = Path.Combine(_env.WebRootPath, mediaFolderRelative);

        // Cria o diretório se não existir
        if (!Directory.Exists(uploadsFolderPathAbsolute))
        {
            Directory.CreateDirectory(uploadsFolderPathAbsolute);
            _logger.LogInformation("Diretório de upload criado: {DirPath}", uploadsFolderPathAbsolute);
        }

        // Gera um nome de arquivo único usando GUID e mantém a extensão original
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePathAbsolute = Path.Combine(uploadsFolderPathAbsolute, uniqueFileName);

        // Salva o arquivo no disco
            _logger.LogInformation("Salvando arquivo em: {FilePath}", filePathAbsolute);
        using (var stream = new FileStream(filePathAbsolute, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
            _logger.LogInformation("Arquivo salvo com sucesso: {FilePath}", filePathAbsolute);

        var relativeUrl = $"/{mediaFolderRelative.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
        return relativeUrl;
    }

        // Limpa caracteres inválidos do nome do arquivo (básico)
        private string SanitizeFileName(string fileName)
        {
            // Remove caracteres inválidos para nomes de arquivo comuns
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedName = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());

            // Opcional: Limitar tamanho, remover espaços extras, etc.
            sanitizedName = sanitizedName.Trim().Replace(" ", "_");
            if (string.IsNullOrWhiteSpace(sanitizedName)) {
                sanitizedName = "_arquivo_sem_nome_"; // Fallback
            }
            return sanitizedName.Length > 100 ? sanitizedName.Substring(0, 100) : sanitizedName; // Limita tamanho
        }

    // Valida o ContentType (MIME type) para tipos específicos (opcional)
    private bool IsValidMediaType(IFormFile file, TipoMensagemEnum expectedType)
    {
            if (string.IsNullOrWhiteSpace(file.ContentType)) return false; // Sem ContentType não dá pra validar

            string contentType = file.ContentType.ToLowerInvariant();

            switch (expectedType)
            {
                case TipoMensagemEnum.Audio:
                    return contentType.StartsWith("audio/"); // Ex: audio/mpeg, audio/ogg, audio/wav
                case TipoMensagemEnum.Video:
                    return contentType.StartsWith("video/"); // Ex: video/mp4, video/webm
                case TipoMensagemEnum.Imagem:
                    return contentType.StartsWith("image/"); // Ex: image/jpeg, image/png, image/gif
                // Não validamos TipoMensagemEnum.Arquivo ou Texto aqui
                default:
                    return false;
            }
    }
}
}