using Microsoft.EntityFrameworkCore;
using TWTodos.Models;

namespace TWTodos.Data
{
    public class CajuTalkContext : DbContext
    {
        public CajuTalkContext(DbContextOptions<CajuTalkContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<SalaChat> SalasChat { get; set; }
        public DbSet<UsuarioSala> UsuariosSala { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("CajuTalk");

            modelBuilder.Entity<UsuarioSala>()
                .HasIndex(us => new { us.ID_Usuario, us.ID_Sala })
                .IsUnique();
        }
    }
}
