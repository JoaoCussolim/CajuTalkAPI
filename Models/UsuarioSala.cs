using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    [Table("Usuario_Sala", Schema = "CajuTalk")]
    public class UsuarioSala
    {
        public int ID { get; set; }

        [ForeignKey("ID_Usuario")]
        public int ID_Usuario { get; set; }

        [ForeignKey("ID_Sala")]
        public int ID_Sala { get; set; }

        public bool Criador { get; set; }
        public bool UsuarioBanido { get; set; }
    }
}
