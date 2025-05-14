using System.ComponentModel.DataAnnotations;

namespace TWTodos.Models 
{
    public class RegisterModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string NomeUsuario { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 3)]
        public string LoginUsuario { get; set; }

        [Required]
        [MinLength(6)]
        public string SenhaUsuario { get; set; }
    }
}