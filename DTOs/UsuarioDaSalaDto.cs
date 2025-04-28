namespace TWTodos.DTOs // Define o namespace onde a classe reside (correto)
{
    // Declara a classe como pública para ser acessível por outros partes do código (como o Controller)
    public class UsuarioDaSalaDto
    {
        public int UsuarioId { get; set; }
        public string LoginUsuario { get; set; }
        public string FotoPerfilURL { get; set; }
        public bool IsCriador { get; set; }
        public bool IsBanido { get; set; }
    }
}