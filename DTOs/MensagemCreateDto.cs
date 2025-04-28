using System.ComponentModel.DataAnnotations;

namespace TWTodos.DTOs
{
    public class MensagemCreateDto
    {
        [Required(ErrorMessage = "O ID da sala é obrigatório.")]
        public int SalaId { get; set; } // Renomeado para clareza (mas mapeia para ID_Sala)

        [Required(ErrorMessage = "O conteúdo da mensagem não pode ser vazio.")]
        [StringLength(2000, ErrorMessage = "A mensagem não pode exceder 2000 caracteres.")]
        public string Conteudo { get; set; }

        [Required(ErrorMessage = "O tipo da mensagem é obrigatório.")]
        [StringLength(50)]
        public string TipoMensagem { get; set; } = "Texto"; // Valor padrão, se aplicável
    }
}