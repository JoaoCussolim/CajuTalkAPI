namespace TWTodos.DTOs
{
    // DTO para representar a relação entre um Usuário e uma Sala
    public class UsuarioSalaDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int SalaId { get; set; }
        public bool IsCriador { get; set; }
        public bool IsBanido { get; set; }
    }
}