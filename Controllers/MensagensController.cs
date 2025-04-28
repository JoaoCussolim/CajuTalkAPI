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

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")] // Rota base /mensagens
    [Authorize] // Geralmente, todas as ações com mensagens exigem login
    public class MensagensController : ControllerBase
    {
        private readonly CajuTalkContext _context;

        public MensagensController(CajuTalkContext context)
        {
            _context = context;
        }

        // --- Helper para obter User ID do Token ---
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

        // Rota Post para enviar uma mensagem dentro de uma sala usando DTO
        [HttpPost]
        public async Task<IActionResult> EnviarMensagem([FromBody] MensagemCreateDto MensagemCreateDto)
        {
            // 1. Obter ID do remetente
            if (!TryGetUserId(out int remetenteId))
            {
                return Unauthorized("Remetente não identificado.");
            }

            // 2. Verificar se a sala existe
            var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == MensagemCreateDto.IDSala);
            if (!salaExiste)
            {
                return NotFound($"Sala com ID {MensagemCreateDto.IDSala} não encontrada.");
            }

            // 3. Verificar se o remetente pertence à sala e não está banido
            //    (essencial para segurança!)
            bool podeEnviar = await _context.UsuarioSala
                .AnyAsync(us => us.ID_Sala == MensagemCreateDto.IDSala &&
                                us.ID_Usuario == remetenteId &&
                                !us.UsuarioBanido); // Não pode estar banido
            if (!podeEnviar)
            {
                return Forbid($"Você não tem permissão para enviar mensagens na sala {MensagemCreateDto.IDSala} (não é membro ou está banido).");
            }


            // 4. Mapear DTO para Entidade
            var mensagem = new Mensagem
            {
                ID_Sala = MensagemCreateDto.IDSala,
                ID_Usuario = remetenteId, // ID do usuário logado
                Conteudo = MensagemCreateDto.Conteudo,
                TipoMensagem = MensagemCreateDto.TipoMensagem,
                DataEnvio = DateTime.UtcNow // Definido pelo servidor, usar UTC
            };

            // 5. Salvar no Banco
            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            // 6. Preparar DTO de resposta (opcional, mas bom retornar a mensagem criada)
            // Precisamos buscar os dados do usuário para o DTO completo
             var remetente = await _context.Usuarios
                                       .Where(u => u.ID == remetenteId)
                                       .Select(u => new { u.LoginUsuario, u.FotoPerfilURL }) // Seleciona só o necessário
                                       .FirstOrDefaultAsync();

            if (remetente == null) {
                 // Isso não deveria acontecer se o usuário estava logado, indica inconsistência
                 return StatusCode(500, "Erro ao buscar dados do remetente após salvar a mensagem.");
            }


            var mensagemDto = new MensagemDto
            {
                Id = mensagem.ID,
                SalaId = mensagem.ID_Sala,
                Conteudo = mensagem.Conteudo,
                DataEnvio = mensagem.DataEnvio,
                TipoMensagem = mensagem.TipoMensagem,
                UsuarioId = mensagem.ID_Usuario,
                 LoginUsuario = remetente.LoginUsuario, // Nome/Login do remetente
                 FotoPerfilURL = remetente.FotoPerfilURL // Foto do remetente
            };

            // 7. Retornar (Ok ou CreatedAtAction se tiver um GET para msg individual)
            return Ok(mensagemDto);
        }

        // Rota get que obtem as mensagens de uma sala usando DTO
        [HttpGet("sala/{idSala}")]
        public async Task<ActionResult<IEnumerable<MensagemDto>>> ObterMensagensPorSala(int idSala)
        {
             // 1. Obter ID do usuário requisitante
            if (!TryGetUserId(out int requestingUserId))
            {
                return Unauthorized("Usuário não identificado.");
            }

            // 2. Verificar se a sala existe
            var salaExiste = await _context.SalasChat.AnyAsync(s => s.ID == idSala);
            if (!salaExiste)
            {
                return NotFound($"Sala com ID {idSala} não encontrada.");
            }

            // 3. Verificar se o usuário pertence à sala para poder ler as mensagens
             bool podeLer = await _context.UsuarioSala
                .AnyAsync(us => us.ID_Sala == idSala &&
                                us.ID_Usuario == requestingUserId &&
                                !us.UsuarioBanido); // Não pode estar banido
            if (!podeLer)
            {
                 // Ou talvez permitir ler se a sala for pública? Decisão de negócio.
                 // var sala = await _context.SalasChat.FindAsync(idSala);
                 // if (sala == null || !sala.Publica) {
                      return Forbid($"Você não tem permissão para ler mensagens na sala {idSala} (não é membro ou está banido).");
                 // }
            }

            // 4. Buscar mensagens e informações do remetente
            var mensagensDto = await _context.Mensagens
                .Where(m => m.ID_Sala == idSala)
                .OrderBy(m => m.DataEnvio) // Ordena pela data de envio
                .Join( // Junta com Usuarios para pegar nome/foto do remetente
                    _context.Usuarios,
                    msg => msg.ID_Usuario, // Chave na Mensagem
                    usr => usr.ID,         // Chave no Usuario
                    (msg, usr) => new MensagemDto // Projeta o resultado no DTO
                    {
                        Id = msg.ID,
                        SalaId = msg.ID_Sala,
                        Conteudo = msg.Conteudo,
                        DataEnvio = msg.DataEnvio,
                        TipoMensagem = msg.TipoMensagem,
                        UsuarioId = msg.ID_Usuario,
                        LoginUsuario = usr.LoginUsuario, // Nome/Login do remetente
                        FotoPerfilURL = usr.FotoPerfilURL  // Foto do remetente
                    })
                .ToListAsync(); // Executa a query

            return Ok(mensagensDto);
        }
    }
}