using System.ComponentModel.DataAnnotations;

namespace TWTodos.DTOs
{
    public class SalaCreateDto
    {
        [Required(ErrorMessage = "O nome da sala é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome da sala deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "É necessário indicar se a sala é pública ou privada.")]
        public bool Publica { get; set; }

        [StringLength(50, ErrorMessage = "A senha não pode exceder 50 caracteres.")]
        public string? Senha { get; set; }

        [Url(ErrorMessage = "A URL da foto de perfil deve ser uma URL válida.")]
        [StringLength(2048, ErrorMessage = "A URL da foto de perfil é muito longa.")]
        public string? FotoPerfilURL { get; set; }
    }
}