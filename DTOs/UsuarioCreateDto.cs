using System.ComponentModel.DataAnnotations; // Para atributos de validação

namespace TWTodos.DTOs
{
    public class UsuarioCreateDto
    {
        [Required(ErrorMessage = "O nome do usuário é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string NomeUsuario { get; set; } = null!; // Inicializa para satisfazer nullable checks

        [Required(ErrorMessage = "O login do usuário é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O login deve ter entre 3 e 50 caracteres.")]
        public string LoginUsuario { get; set; } = null!; // Inicializa

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string SenhaUsuario { get; set; } = null!; // Recebe a senha em texto plano

        // FotoPerfilURL geralmente é definida como padrão na criação, então não precisa vir aqui.
    }
}