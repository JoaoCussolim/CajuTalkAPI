using System.ComponentModel.DataAnnotations; // <--- ADICIONE ESTA LINHA

namespace TWTodos.Models // Certifique-se que o namespace está correto
{
    public class RegisterModel
    {
        [Required] // Agora será reconhecido
        [StringLength(50, MinimumLength = 3)] // Agora será reconhecido
        public string NomeUsuario { get; set; }

        [Required] // Agora será reconhecido
        [StringLength(30, MinimumLength = 3)]
        public string LoginUsuario { get; set; }

        [Required] // Agora será reconhecido
        [MinLength(6)] // Agora será reconhecido
        public string SenhaUsuario { get; set; }
    }
}