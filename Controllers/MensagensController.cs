using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MensagensController : ControllerBase
    {
        private readonly CajuTalkContext _context;

        public MensagensController(CajuTalkContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensagem([FromBody] Mensagem mensagem)
        {
            mensagem.DataEnvio = DateTime.Now;
            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            return Ok(mensagem);
        }

        [HttpGet("sala/{idSala}")]
        public async Task<ActionResult<IEnumerable<Mensagem>>> ObterMensagensPorSala(int idSala)
        {
            var mensagens = await _context.Mensagens
                .Where(m => m.ID_Sala == idSala)
                .OrderBy(m => m.DataEnvio)
                .ToListAsync();

            return mensagens;
        }
    }
}
