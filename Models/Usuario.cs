using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    // Tabela Usuário do Esquema CajuTalk
    [Table("Usuario", Schema = "CajuTalk")]
    // Classe da Tabela Usuario
    public class Usuario
    {
        // Atributos:
        public int ID { get; set; }
        public string NomeUsuario { get; set; }
        public string LoginUsuario { get; set; }
        public string SenhaHash { get; set; }
        public string? FotoPerfilURL { get; set; }
        public string? CorFundo { get; set; }
    }
}
