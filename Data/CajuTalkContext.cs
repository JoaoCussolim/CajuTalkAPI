using Microsoft.EntityFrameworkCore;
using TWTodos.Models;

namespace TWTodos.Data
{
    public class CajuTalkContext : DbContext
    {
        public CajuTalkContext(DbContextOptions<CajuTalkContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }            // cria a tabela Usuarios
        public DbSet<SalaChat> SalasChat { get; set; }          // cria a tabela SalasChat
        public DbSet<UsuarioSala> UsuariosSala { get; set; }    // cria a tabela UsuariosSala
        public DbSet<Mensagem> Mensagens { get; set; }          // cria a tabela Mensagens

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("CajuTalk");          // cria o schema cajutalk

            modelBuilder.Entity<UsuarioSala>()
                .HasIndex(us => new { us.ID_Usuario, us.ID_Sala })
                .IsUnique();
        }
    }
}
