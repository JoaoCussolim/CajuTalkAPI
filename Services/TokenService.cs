// In Services/TokenService.cs
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.IdentityModel.Tokens;     // Required for SecurityKey, SigningCredentials etc.
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;    // Required for JwtSecurityTokenHandler, JwtRegisteredClaimNames
using System.Security.Claims;             // Required for Claim
using System.Text;                        // Required for Encoding
using TWTodos.Models;                     // Assuming Usuario is in this namespace

namespace TWTodos.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _key; // Store the key derived from the secret

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            // Get the secret key from configuration and create a SymmetricSecurityKey
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        }

        public string GenerateToken(Usuario usuario)
        {
            // 1. Define Claims (information about the user embedded in the token)
            var claims = new List<Claim>
            {
                // Standard claims (defined in JwtRegisteredClaimNames)
                new Claim(JwtRegisteredClaimNames.NameId, usuario.ID.ToString()), // User ID
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.LoginUsuario), // Username (often Login)
                new Claim(JwtRegisteredClaimNames.GivenName, usuario.NomeUsuario), // User's actual name
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token identifier

                // You can add custom claims here if needed (e.g., roles)
                // new Claim(ClaimTypes.Role, "Admin"),
                // new Claim("customClaim", "customValue")
            };

            // 2. Define Signing Credentials
            // Use the key generated in the constructor and specify the algorithm
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature); // Or HmacSha512 if key is long enough

            // 3. Define Token Descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = creds
            };

            // 4. Create Token Handler and Generate Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 5. Write Token to String format
            return tokenHandler.WriteToken(token);
        }
    }
}