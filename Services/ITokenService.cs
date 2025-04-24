using TWTodos.Models; // Assuming Usuario is in this namespace

namespace TWTodos.Services
{
    public interface ITokenService
    {
        string GenerateToken(Usuario usuario);
    }
}