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

        [HttpPost]
        public async Task<IActionResult> CriarSala([FromBody] SalaChat sala)
        {
            _context.SalasChat.Add(sala);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = sala.ID }, sala);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaChat>>> ObterTodas()
        {
            return await _context.SalasChat.ToListAsync();
        }

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