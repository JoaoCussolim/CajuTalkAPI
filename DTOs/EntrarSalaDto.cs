using System.ComponentModel.DataAnnotations;

namespace TWTodos.DTOs
{
    public class EntrarSalaDto
    {
        [Required]
        public int SalaId { get; set; }

        public string? Senha { get; set; }
    }
}