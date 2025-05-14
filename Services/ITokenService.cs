using TWTodos.Models;

namespace TWTodos.Services
{
    public interface ITokenService
    {
        string GenerateToken(Usuario usuario);
    }
}