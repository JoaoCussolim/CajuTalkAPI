using System.ComponentModel.DataAnnotations;

namespace TWTodos.DTOs
{
    public class EntrarSalaDto
    {
        [Required]
        public int SalaId { get; set; }

        // Poderia incluir a senha aqui se a sala for privada e exigir senha na entrada
        public string? Senha { get; set; }
    }
}