using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Models;

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

        // Rota Post para criar salas dentro do banco de dados
        [HttpPost]
        public async Task<IActionResult> CriarSala([FromBody] SalaChat sala)
        {
            _context.SalasChat.Add(sala);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = sala.ID }, sala);
        }

        // Rota Get para obter todas as salas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaChat>>> ObterTodas()
        {
            return await _context.SalasChat.ToListAsync();
        }

        // Rota Get que obtem uma sala pelo seu ID
        [HttpGet("{id}")]
        public async Task<ActionResult<SalaChat>> ObterPorId(int id)
        {
            var sala = await _context.SalasChat.FindAsync(id);

            if (sala == null)
                return NotFound();

            return sala;
        }
    }
}