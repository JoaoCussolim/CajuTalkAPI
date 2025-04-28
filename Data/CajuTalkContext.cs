using Microsoft.EntityFrameworkCore;
using TWTodos.Models;

namespace TWTodos.Data
{
    public class CajuTalkContext : DbContext
    {
        public CajuTalkContext(DbContextOptions<CajuTalkContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }            // cria a variavel que representa a tabela Usuarios 
        public DbSet<SalaChat> SalasChat { get; set; }          // cria a variavel que representa a tabela SalasChat
        public DbSet<UsuarioSala> UsuarioSala { get; set; }    // cria a variavel que representa a tabela UsuariosSala
        public DbSet<Mensagem> Mensagens { get; set; }          // cria a variavel que representa a tabela Mensagens

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("CajuTalk");          // define o schema cajutalk

            modelBuilder.Entity<UsuarioSala>()
                .HasIndex(us => new { us.ID_Usuario, us.ID_Sala })
                .IsUnique();
        }
    }
}
