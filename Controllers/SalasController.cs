    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using TWTodos.Data;          // Contexto do banco de dados
    using TWTodos.Models;        // Modelos de entidade (SalaChat, UsuarioSala, Usuario)
    using TWTodos.DTOs;          // Nossos DTOs
    using System.Security.Claims; // Para ClaimTypes e User.FindFirst
    using Microsoft.AspNetCore.Authorization; // Para [Authorize] e [AllowAnonymous]
    using System.Collections.Generic; // Para IEnumerable, List
    using System.Linq; // Para LINQ (Where, Select, Join, RemoveRange)
    using System.Threading.Tasks;   // Para Task<>

    namespace TWTodos.Controllers
    {
        [ApiController]
        // Mantendo a rota base como /[controller] -> /salas
        [Route("[controller]")]
        public class SalasController : ControllerBase
        {
            private readonly CajuTalkContext _context;

            public SalasController(CajuTalkContext context)
            {
                _context = context;
            }

            // --- Helper para obter User ID do Token ---
            // (Movido para cá para ser reutilizado)
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


            // --- Rota POST para criar salas ---
            // Requer que o usuário esteja autenticado para criar uma sala.
            [HttpPost]
            [Authorize]
            // Corrigindo o nome do DTO aqui para corresponder ao corpo do método
            public async Task<IActionResult> CriarSala([FromBody] SalaCreateDto SalaCreateDto)
            {
                // 1. Obter o ID do Criador
                if (!TryGetUserId(out int criadorId))
                {
                    // Esta verificação pode ser redundante devido ao [Authorize], mas é segura
                    return Unauthorized("Usuário não identificado ou token inválido.");
                }

                // 2. Validação Adicional
                if (!SalaCreateDto.Publica && string.IsNullOrWhiteSpace(SalaCreateDto.Senha))
                {
                    ModelState.AddModelError(nameof(SalaCreateDto.Senha), "Uma senha é obrigatória para salas privadas.");
                    return ValidationProblem(ModelState);
                }
                if (SalaCreateDto.Publica && !string.IsNullOrWhiteSpace(SalaCreateDto.Senha))
                {
                    SalaCreateDto.Senha = null;
                }

                // 3. Mapear DTO para Entidade
                var sala = new SalaChat
                {
                    Nome = SalaCreateDto.Nome,
                    Publica = SalaCreateDto.Publica,
                    Senha = SalaCreateDto.Senha, // Usar a senha do DTO (INSEGURO se não for hash)
                    FotoPerfilURL = SalaCreateDto.FotoPerfilURL,
                    CriadorID = criadorId
                };

                // 4. Salvar Sala no Banco
                _context.SalasChat.Add(sala);
                await _context.SaveChangesAsync();

                // 5. [NOVO] Adicionar o criador à tabela UsuarioSala automaticamente
                var criadorRelacao = new UsuarioSala
                {
                    ID_Usuario = criadorId,
                    ID_Sala = sala.ID, // ID da sala recém-criada
                    Criador = true,
                    UsuarioBanido = false
                };
                _context.UsuarioSala.Add(criadorRelacao);
                await _context.SaveChangesAsync(); // Salva a relação do criador

                // 6. Mapear Entidade para DTO de Resposta
                var salaDto = new SalaChatDto
                {
                    ID = sala.ID,
                    Nome = sala.Nome,
                    Publica = sala.Publica,
                    FotoPerfilURL = sala.FotoPerfilURL,
                    CriadorID = sala.CriadorID
                };

                // 7. Retornar Resposta 201 Created
                return CreatedAtAction(nameof(ObterPorId), new { id = sala.ID }, salaDto);
            }

            // --- Rota GET para obter todas as salas ---
            [HttpGet]
            [AllowAnonymous]
            public async Task<ActionResult<IEnumerable<SalaChatDto>>> ObterTodas()
            {
                var salasDto = await _context.SalasChat
                    .Select(s => new SalaChatDto // Mapeie as propriedades aqui!
                    {
                        ID = s.ID,
                        Nome = s.Nome,
                        Publica = s.Publica,
                        FotoPerfilURL = s.FotoPerfilURL, // Certifique-se que DTO.FotoPerfilURL pode ser nulo (string?) se Model.FotoPerfilURL for nulo
                        CriadorID = s.CriadorID
                    })
                    .ToListAsync();
                return Ok(salasDto);
            }

            // --- Rota GET para obter uma sala pelo seu ID ---
            [HttpGet("{id}")]
            [AllowAnonymous]
            public async Task<ActionResult<SalaChatDto>> ObterPorId(int id)
            {
                var sala = await _context.SalasChat.FindAsync(id);
                if (sala == null) return NotFound($"Sala com ID {id} não encontrada.");

                // Mapeie as propriedades da 'sala' encontrada para o DTO
                var salaDto = new SalaChatDto
                {
                    ID = sala.ID,
                    Nome = sala.Nome,
                    Publica = sala.Publica,
                    FotoPerfilURL = sala.FotoPerfilURL, 
                    CriadorID = sala.CriadorID
                };
                return Ok(salaDto);
            }


            // --- Rota GET para obter os usuários de uma sala específica ---
            // (Adicionada conforme solicitado anteriormente)
            [HttpGet("{salaId}/usuarios")]
            [Authorize] // Exige autenticação para ver a lista de usuários
            public async Task<ActionResult<IEnumerable<UsuarioDaSalaDto>>> GetUsuariosDaSala(int salaId)
            {
                // 1. Verificar se a sala existe
                var sala = await _context.SalasChat.FindAsync(salaId);
                if (sala == null)
                {
                    return NotFound($"Sala com ID {salaId} não encontrada.");
                }

                // 2. Verificar permissão (se privada, só membros podem ver)
                if (!sala.Publica)
                {
                    if (!TryGetUserId(out int requestingUserId))
                    {
                        return Unauthorized("Usuário não identificado."); // Embora [Authorize] deva pegar
                    }

                    bool isMember = await _context.UsuarioSala
                        .AnyAsync(us => us.ID_Sala == salaId && us.ID_Usuario == requestingUserId && !us.UsuarioBanido);

                    if (!isMember)
                    {
                        return Forbid($"Acesso negado à lista de usuários da sala privada {salaId}.");
                    }
                }

                // 3. Consultar e mapear usuários da sala
                // Assumindo que existe _context.Usuarios e que Usuario tem ID, LoginUsuario, FotoPerfilURL
                var usuariosDaSalaDto = await _context.UsuarioSala
                    .Where(us => us.ID_Sala == salaId)
                    .Join(
                        _context.Usuarios,
                        us => us.ID_Usuario,
                        u => u.ID,
                        (us, u) => new UsuarioDaSalaDto
                        {
                            UsuarioId = u.ID,
                            LoginUsuario = u.LoginUsuario, // Ajuste se o nome da propriedade for diferente
                            FotoPerfilURL = u.FotoPerfilURL, // Ajuste se existir/tiver outro nome
                            IsCriador = us.Criador,
                            IsBanido = us.UsuarioBanido
                        })
                    .ToListAsync();

                // 4. Retornar a lista
                return Ok(usuariosDaSalaDto);
            }


            [HttpDelete("{id}")]
            [Authorize]
            public async Task<IActionResult> DeletarSala(int id)
            {
                if (!TryGetUserId(out int requestingUserId)) { /* ... */ }

                var sala = await _context.SalasChat.FindAsync(id);
                if (sala == null)
                {
                    return NotFound($"Sala com ID {id} não encontrada (ou já foi deletada).");
                }

                if (sala.CriadorID != requestingUserId) { /* ... */ }

                _context.SalasChat.Remove(sala);

                try
                {
                    await _context.SaveChangesAsync();
                    return NoContent(); // Success
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency exception deleting Sala ID {id}: {ex.Message}");

                    var exists = await _context.SalasChat.AnyAsync(s => s.ID == id);
                    if (!exists)
                    {
                        return NoContent();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Erro de concorrência inesperado ao deletar sala.");
                    }

                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"Database error deleting Sala ID {id}: {dbEx.ToString()}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erro no banco de dados ao deletar sala.");
                }
            }
        }
    }