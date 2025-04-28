using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    [Table("Mensagem", Schema = "CajuTalk")]
    public class Mensagem
    {
        public int ID { get; set; }

        [ForeignKey("ID_Sala")]
        public int ID_Sala { get; set; }

        [ForeignKey("ID_Usuario")]
        public int ID_Usuario { get; set; }

        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
        public string TipoMensagem { get; set; }
    }
}