using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;
using TWTodos.DTOs; // Nossos DTOs
using System.Security.Claims; // Para ClaimTypes
using Microsoft.AspNetCore.Authorization; // Para [Authorize]
using System.Threading.Tasks;
using System.Linq; // Para AnyAsync, FirstOrDefaultAsync

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // Todas as ações neste controller exigem autenticação
    public class UsuarioSalaController : ControllerBase
    {
        private readonly CajuTalkContext _context;


        public UsuarioSalaController(CajuTalkContext context)
        {
            _context = context;
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

        [HttpPost("entrar")]
        public async Task<IActionResult> EntrarSala([FromBody] EntrarSalaDto entrarDto)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("Usuário não identificado.");
            }

            var sala = await _context.SalasChat.FindAsync(entrarDto.SalaId);
            if (sala == null)
            {
                return NotFound($"Sala com ID {entrarDto.SalaId} não encontrada.");
            }

            bool jaExiste = await _context.UsuarioSala
                .AnyAsync(us => us.ID_Usuario == userId && us.ID_Sala == entrarDto.SalaId);

            if (jaExiste)
            {
                // O usuário já está na sala, verificar se está banido
                 var relacaoExistente = await _context.UsuarioSala
                    .FirstOrDefaultAsync(us => us.ID_Usuario == userId && us.ID_Sala == entrarDto.SalaId);

                 if (relacaoExistente != null && relacaoExistente.UsuarioBanido) {
                     return Forbid($"Você está banido da sala '{sala.Nome}'."); // 403 Forbidden
                 }
                return Ok(new UsuarioSalaDto { // Exemplo de retorno
                     Id = relacaoExistente.ID,
                     UsuarioId = relacaoExistente.ID_Usuario,
                     SalaId = relacaoExistente.ID_Sala,
                     IsCriador = relacaoExistente.Criador,
                     IsBanido = relacaoExistente.UsuarioBanido
                 });
            }

            // Verificar se a sala é privada e requer senha
            if (!sala.Publica)
            {
                if (string.IsNullOrWhiteSpace(sala.Senha)) {
                    return BadRequest("Esta sala privada não pode ser acessada no momento.");
                }

                if (string.IsNullOrWhiteSpace(entrarDto.Senha)) {
                    return BadRequest("Senha é obrigatória para entrar nesta sala privada.");
                }

                if (sala.Senha != entrarDto.Senha)
                {
                    return Unauthorized("Senha da sala incorreta.");
                }
            }


            // Criar a nova relação UsuarioSala
            var novaRelacao = new UsuarioSala
            {
                ID_Usuario = userId,
                ID_Sala = entrarDto.SalaId,
                Criador = false,
                UsuarioBanido = false
            };

            _context.UsuarioSala.Add(novaRelacao);
            await _context.SaveChangesAsync();

            // Mapear para DTO para retornar
            var dtoResultado = new UsuarioSalaDto
            {
                Id = novaRelacao.ID,
                UsuarioId = novaRelacao.ID_Usuario,
                SalaId = novaRelacao.ID_Sala,
                IsCriador = novaRelacao.Criador,
                IsBanido = novaRelacao.UsuarioBanido
            };

            // Retornar 201 Created ou 200 OK com o DTO da relação criada
            // Usar CreatedAtAction precisaria de um endpoint GET para uma relação específica. Ok é mais simples aqui.
            return Ok(dtoResultado);
        }

        // --- Sair de uma Sala ---
        [HttpDelete("sair/{salaId}")] // DELETE /api/usuariosala/sair/{salaId}
        public async Task<IActionResult> SairSala(int salaId)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("Usuário não identificado.");
            }

            // Encontrar a relação existente
            var relacao = await _context.UsuarioSala
                .FirstOrDefaultAsync(us => us.ID_Usuario == userId && us.ID_Sala == salaId);

            if (relacao == null)
            {
                return NotFound($"Você não pertence à sala com ID {salaId}.");
            }

            if (relacao.Criador)
            {
                return BadRequest("O criador não pode sair da sala por esta rota. Considere excluir a sala.");
            }

            // Remover a relação
            _context.UsuarioSala.Remove(relacao);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content é o padrão para DELETE bem-sucedido
        }

        [HttpPut("{salaId}/usuarios/{usuarioIdParaBanir}/banir")]
        public async Task<IActionResult> BanirUsuario(int salaId, int usuarioIdParaBanir)
        {
            if (!TryGetUserId(out int adminUserId))
            {
                return Unauthorized("Usuário requisitante não identificado.");
            }

            // Verificar se o requisitante tem permissão (é o criador da sala)
            var sala = await _context.SalasChat.FindAsync(salaId);
            if (sala == null)
            {
                return NotFound($"Sala com ID {salaId} não encontrada.");
            }
            if (sala.CriadorID != adminUserId)
            {
                 // Poderia adicionar lógica para roles de "Admin" aqui também
                return Forbid("Apenas o criador da sala pode banir usuários."); // 403 Forbidden
            }

            // Impedir que o criador se bana ou bana a si mesmo (redundante se já checou acima, mas seguro)
            if (usuarioIdParaBanir == adminUserId)
            {
                return BadRequest("O criador não pode banir a si mesmo.");
            }

            // Encontrar a relação do usuário a ser banido
            var relacaoAlvo = await _context.UsuarioSala
                .FirstOrDefaultAsync(us => us.ID_Usuario == usuarioIdParaBanir && us.ID_Sala == salaId);

            if (relacaoAlvo == null)
            {
                 return NotFound($"Usuário com ID {usuarioIdParaBanir} não encontrado na sala {salaId}.");
            }

            // Impedir banimento do criador (caso o alvo seja o criador por algum motivo)
            if (relacaoAlvo.Criador) {
                 return BadRequest("Não é possível banir o criador da sala.");
            }

            // Aplicar o banimento
            if (relacaoAlvo.UsuarioBanido) {
                return Ok($"Usuário {usuarioIdParaBanir} já estava banido da sala {salaId}."); // Ou apenas Ok()
            }

            relacaoAlvo.UsuarioBanido = true;
            _context.UsuarioSala.Update(relacaoAlvo);
            await _context.SaveChangesAsync();

             var dtoResultado = new UsuarioSalaDto {
                 Id = relacaoAlvo.ID, UsuarioId = relacaoAlvo.ID_Usuario, SalaId = relacaoAlvo.ID_Sala,
                 IsCriador = relacaoAlvo.Criador, IsBanido = relacaoAlvo.UsuarioBanido
             };
            return Ok(dtoResultado); // Retorna 200 OK com o estado atualizado
        }

        // --- Desbanir um Usuário de uma Sala (Ação de Admin/Criador) ---
        [HttpPut("{salaId}/usuarios/{usuarioIdParaDesbanir}/desbanir")] // PUT /api/usuariosala/{salaId}/usuarios/{usuarioIdParaDesbanir}/desbanir
        public async Task<IActionResult> DesbanirUsuario(int salaId, int usuarioIdParaDesbanir)
        {
             if (!TryGetUserId(out int adminUserId))
            {
                return Unauthorized("Usuário requisitante não identificado.");
            }

            // Verificar permissão (Criador)
            var sala = await _context.SalasChat.FindAsync(salaId);
            if (sala == null) return NotFound($"Sala com ID {salaId} não encontrada.");
            if (sala.CriadorID != adminUserId) return Forbid("Apenas o criador da sala pode desbanir usuários.");

             // Encontrar a relação do usuário a ser desbanido
            var relacaoAlvo = await _context.UsuarioSala
                .FirstOrDefaultAsync(us => us.ID_Usuario == usuarioIdParaDesbanir && us.ID_Sala == salaId);

             if (relacaoAlvo == null)
            {
                 return NotFound($"Usuário com ID {usuarioIdParaDesbanir} não encontrado na sala {salaId} (ou não possui registro de relação).");
            }

             // Aplicar o desbanimento
            if (!relacaoAlvo.UsuarioBanido) {
                return Ok($"Usuário {usuarioIdParaDesbanir} não estava banido da sala {salaId}."); // Ou apenas Ok()
            }

             relacaoAlvo.UsuarioBanido = false;
            _context.UsuarioSala.Update(relacaoAlvo);
            await _context.SaveChangesAsync();

             var dtoResultado = new UsuarioSalaDto {
                 Id = relacaoAlvo.ID, UsuarioId = relacaoAlvo.ID_Usuario, SalaId = relacaoAlvo.ID_Sala,
                 IsCriador = relacaoAlvo.Criador, IsBanido = relacaoAlvo.UsuarioBanido
             };
            return Ok(dtoResultado);
        }
        
        [HttpGet("salas/{salaId}/usuarios/{usuarioId}")]
        public async Task<ActionResult<UsuarioSalaDto>> GetRelacaoUsuarioSala(int salaId, int usuarioId)
        {
            if (!TryGetUserId(out int requestingUserId))
            {
                 return Unauthorized("Usuário requisitante não identificado.");
            }

            var relacao = await _context.UsuarioSala
                                    .AsNoTracking() // Bom para leitura apenas
                                    .FirstOrDefaultAsync(us => us.ID_Sala == salaId && us.ID_Usuario == usuarioId);

            if (relacao == null)
            {
                return NotFound($"Nenhuma relação encontrada para o usuário {usuarioId} na sala {salaId}.");
            }

            // Verificar permissão para ver a relação
            // Ex: Só o próprio usuário ou o criador da sala podem ver?
            var sala = await _context.SalasChat.FindAsync(salaId); // Precisa para verificar criador
            if (sala == null) return NotFound(); // Sala sumiu?

            if (requestingUserId != usuarioId && requestingUserId != sala.CriadorID)
            {
                return Forbid("Você não tem permissão para ver os detalhes desta relação.");
            }

            var dto = new UsuarioSalaDto
            {
                Id = relacao.ID,
                UsuarioId = relacao.ID_Usuario,
                SalaId = relacao.ID_Sala,
                IsCriador = relacao.Criador,
                IsBanido = relacao.UsuarioBanido
            };

            return Ok(dto);
        }
    }
}