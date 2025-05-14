namespace TWTodos.DTOs
{
    public class SalaChatDto
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public bool Publica { get; set; }
        public string? FotoPerfilURL { get; set; }
        public int CriadorID { get; set; } // É seguro expor o ID do criador
    }
}