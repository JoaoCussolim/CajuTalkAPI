using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    // tabela SalaChat do esquema Cajutalk
    [Table("SalaChat", Schema = "CajuTalk")]
    // classe da tabela SalaChat
    public class SalaChat
    {
        // Atributos:
        public int ID { get; set; }
        public string Nome { get; set; }
        public bool Publica { get; set; }
        public string? Senha { get; set; }
        public string? FotoPerfilURL { get; set; }

        // Atributo Foreign key:
        [ForeignKey("CriadorID")]
        public int CriadorID { get; set; }
    }
}
