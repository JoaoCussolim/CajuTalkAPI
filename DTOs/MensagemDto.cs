namespace TWTodos.DTOs
{
    public class MensagemDto
    {
        public int Id { get; set; } // ID da mensagem
        public int SalaId { get; set; } // ID da sala onde foi enviada

        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
        public TipoMensagemEnum TipoMensagem { get; set; }

        // Informações do Remetente (essenciais)
        public int UsuarioId { get; set; }
        public string LoginUsuario { get; set; }
        public string? FotoPerfilURL {get; set;}
        public IFormFile? MediaFile { get; set; }

    }
}