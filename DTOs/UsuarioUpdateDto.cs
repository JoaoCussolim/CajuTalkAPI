using Microsoft.AspNetCore.Http; // Para IFormFile
using System.ComponentModel.DataAnnotations;

namespace TWTodos.DTOs
{
    public class UsuarioUpdateDto
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string? NomeUsuario { get; set; } // Nullable permite atualizações parciais

        [StringLength(50, MinimumLength = 3, ErrorMessage = "O login deve ter entre 3 e 50 caracteres.")]
        public string? LoginUsuario { get; set; } // Nullable

        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string? SenhaUsuario { get; set; } // Nullable. Recebe a NOVA senha em texto plano

        public string? Recado { get; set; }

        public string? NovaFotoPerfil { get; set; }
    }
}