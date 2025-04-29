using System.ComponentModel.DataAnnotations;

namespace TWTodos.Models
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}