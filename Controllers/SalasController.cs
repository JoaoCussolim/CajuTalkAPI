using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;          // Contexto do banco de dados
using TWTodos.Models;        // Modelos de entidade (SalaChat)
using TWTodos.DTOs;          // Nossos DTOs (SalaCreateDto, SalaChatDto)
using System.Security.Claims; // Para ClaimTypes e User.FindFirst
using Microsoft.AspNetCore.Authorization; // Para [Authorize] e [AllowAnonymous]
using System.Collections.Generic; // Para IEnumerable
using System.Threading.Tasks;   // Para Task<>

namespace TWTodos.Controllers
{
    [ApiController]
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

            // --- Rota GET para obter os usuários de uma sala específica ---
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
                var usuariosDaSalaDto = await _context.UsuarioSala
                    .Where(us => us.ID_Sala == salaId)
                    .Join(
                        _context.Usuarios,
                        us => us.ID_Usuario,
                        u => u.ID,
                        (us, u) => new UsuarioDaSalaDto
                        {
                            UsuarioId = u.ID,
                            LoginUsuario = u.LoginUsuario,
                            FotoPerfilURL = u.FotoPerfilURL,
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

            [HttpPost]
            [Authorize]
            public async Task<IActionResult> CriarSala([FromBody] SalaCreateDto SalaCreateDto)
            {
                // 1. Obter o ID do Criador
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized("Claim de identificador do usuário não encontrado no token.");
                }
                if (!int.TryParse(userIdClaim.Value, out int criadorId))
                {
                    return BadRequest("O formato do ID do usuário no token é inválido.");
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

                // 3. Mapear DTO para Entidade SalaChat
                var sala = new SalaChat
                {
                    Nome = SalaCreateDto.Nome,
                    Publica = SalaCreateDto.Publica,
                    Senha = SalaCreateDto.Senha,
                    FotoPerfilURL = SalaCreateDto.FotoPerfilURL,
                    CriadorID = criadorId
                };

                // 4. Salvar a SalaChat no Banco de Dados
                _context.SalasChat.Add(sala);
                await _context.SaveChangesAsync(); // <--- PRIMEIRO SAVE (obtém sala.ID)

                try // É bom ter um try-catch aqui para o caso de falha na segunda inserção
                {
                    var associacaoCriador = new UsuarioSala
                    {
                        ID_Usuario = criadorId,   // ID do usuário que criou
                        ID_Sala = sala.ID,        // ID da sala que acabou de ser criada
                        Criador = true,           // Marca como criador na tabela de associação
                        UsuarioBanido = false     // Garante que o criador não começa banido
                    };
                    _context.UsuarioSala.Add(associacaoCriador);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao associar o criador à sala após a criação.");
                }

                // 5. Mapear Entidade para DTO de Resposta (como antes)
                var salaDto = new SalaChatDto
                {
                    ID = sala.ID,
                    Nome = sala.Nome,
                    Publica = sala.Publica,
                    FotoPerfilURL = sala.FotoPerfilURL,
                    CriadorID = sala.CriadorID
                };

                // 6. Retornar Resposta 201 Created (como antes)
                return Created($"/salas/{sala.ID}", salaDto);
            }

        // --- Rota GET para obter todas as salas ---
        // Permite acesso anônimo (não precisa estar logado).
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SalaChatDto>>> ObterTodas()
        {
            // Mapeia diretamente para DTO na consulta para eficiência
            var salasDto = await _context.SalasChat
                .Select(s => new SalaChatDto
                {
                    ID = s.ID,
                    Nome = s.Nome,
                    Publica = s.Publica,
                    FotoPerfilURL = s.FotoPerfilURL,
                    CriadorID = s.CriadorID
                })
                .ToListAsync();

            return Ok(salasDto); // Retorna 200 OK com a lista de DTOs
        }

        // --- Rota GET para obter uma sala pelo seu ID ---
        // Permite acesso anônimo.
        [HttpGet("{id}")]
        [AllowAnonymous] // Ou [Authorize] se necessário
        public async Task<ActionResult<SalaChatDto>> ObterPorId(int id)
        {
            // Busca a entidade no banco
            var sala = await _context.SalasChat.FindAsync(id);

            if (sala == null)
            {
                return NotFound($"Sala com ID {id} não encontrada."); // Retorna 404 Not Found
            }

            // Mapeia a entidade encontrada para o DTO de resposta
            var salaDto = new SalaChatDto
            {
                ID = sala.ID,
                Nome = sala.Nome,
                Publica = sala.Publica,
                FotoPerfilURL = sala.FotoPerfilURL,
                CriadorID = sala.CriadorID
            };

            return Ok(salaDto); // Retorna 200 OK com o DTO da sala
        }
    }
}
