using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    [Table("Usuario", Schema = "CajuTalk")]
    public class Usuario
    {
        public int ID { get; set; }
        public string NomeUsuario { get; set; }
        public string LoginUsuario { get; set; }
        public string SenhaHash { get; set; }
        public string? FotoPerfilURL { get; set; }
        public string? CorFundo { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
