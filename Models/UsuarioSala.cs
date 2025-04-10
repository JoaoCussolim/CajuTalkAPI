using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    // Tabela Usuario_sala do esquema CajuTalk
    [Table("Usuario_Sala", Schema = "CajuTalk")]
    // Classe da tabela Usuario_Sala
    public class UsuarioSala
    {
        // Atributo:
        public int ID { get; set; }

        // Atributo Foreign Key:
        [ForeignKey("ID_Usuario")]
        public int ID_Usuario { get; set; }

        // Atributo Foreign Key:
        [ForeignKey("ID_Sala")]
        public int ID_Sala { get; set; }

        // Atributos:
        public bool Criador { get; set; }
        public bool UsuarioBanido { get; set; }
    }
}
