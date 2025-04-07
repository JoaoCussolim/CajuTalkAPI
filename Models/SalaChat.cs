using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    [Table("SalaChat", Schema = "CajuTalk")]
    public class SalaChat
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public bool Publica { get; set; }
        public string? Senha { get; set; }
        public string? FotoPerfilURL { get; set; }

        [ForeignKey("CriadorID")]
        public int CriadorID { get; set; }
    }
}
