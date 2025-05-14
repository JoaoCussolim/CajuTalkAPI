namespace TWTodos.DTOs
{
    public class UsuarioDaSalaDto
    {
        public int UsuarioId { get; set; }
        public string LoginUsuario { get; set; }
        public string FotoPerfilURL { get; set; }
        public bool IsCriador { get; set; }
        public bool IsBanido { get; set; }
    }
}