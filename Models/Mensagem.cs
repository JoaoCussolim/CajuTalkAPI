using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    // Tabela Mensagem do Schema CajuTalk
    [Table("Mensagem", Schema = "CajuTalk")]
    // classe da tabela mensagem
    public class Mensagem
    {
        // atributo:
        public int ID { get; set; }

        // atributo foreign key:
        [ForeignKey("ID_Sala")]
        public int ID_Sala { get; set; }
        
        // atributos:
        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.Now;
        public string TipoMensagem { get; set; }
    }
}
