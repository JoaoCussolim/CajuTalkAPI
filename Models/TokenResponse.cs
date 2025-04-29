namespace TWTodos.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty; // Inicializar para evitar CS8618
        public string RefreshToken { get; set; } = string.Empty; // Inicializar para evitar CS8618
        public DateTime AccessTokenExpiration { get; set; }
    }
}