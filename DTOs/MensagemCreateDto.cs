using Microsoft.AspNetCore.Http; // Necessário para IFormFile
using System.ComponentModel.DataAnnotations; // Para atributos de validação
using TWTodos.Models; // Ou onde quer que seu TipoMensagemEnum esteja definido

namespace TWTodos.DTOs
{
    public class MensagemCreateDto
    {
        [Required(ErrorMessage = "O ID da sala é obrigatório.")]
        public int IDSala { get; set; }

        // O conteúdo é opcional para arquivos, mas obrigatório para texto.
        // A validação de obrigatoriedade para texto será feita no controller.
        public string? Conteudo { get; set; }

        [Required(ErrorMessage = "O tipo da mensagem é obrigatório.")]
        // Certifique-se que o Enum TipoMensagemEnum está definido e acessível
        // (ex: em TWTodos.Models ou TWTodos.DTOs)
        public TipoMensagemEnum TipoMensagem { get; set; }

        // Esta propriedade receberá o arquivo enviado pelo cliente.
        // É nullable porque mensagens do tipo Texto não terão arquivo.
        public IFormFile? MediaFile { get; set; }
    }
}