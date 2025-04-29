using System.ComponentModel.DataAnnotations;

namespace TWTodos.Models
{
    public class LoginModel
    {
        [Required] // Agora será reconhecido
        public string LoginUsuario { get; set; }

        [Required] // Agora será reconhecido
        public string SenhaUsuario { get; set; }
    }
}