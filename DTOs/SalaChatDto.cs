namespace TWTodos.DTOs
{
    // DTO para retornar informações da sala (sem a senha)
    public class SalaChatDto
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public bool Publica { get; set; }
        public string? FotoPerfilURL { get; set; }
        public int CriadorID { get; set; } // É seguro expor o ID do criador

        // Note que a Senha NÃO está incluída neste DTO por segurança.
    }
}