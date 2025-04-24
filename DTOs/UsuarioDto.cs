namespace TWTodos.DTOs // Certifique-se que o namespace está correto
{
    public class UsuarioDto
    {
        public int ID { get; set; }
        public required string NomeUsuario { get; set; } // Use 'required' ou inicialize = string.Empty;
        public required string LoginUsuario { get; set; } // Use 'required' ou inicialize = string.Empty;
        public string? FotoPerfilURL { get; set; } // URL pode ser nula/vazia
        public string? CorFundo { get; set; }
    }
}