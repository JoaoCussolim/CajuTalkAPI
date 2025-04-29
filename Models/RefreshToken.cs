using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TWTodos.Models
{
    [Table("RefreshToken", Schema = "CajuTalk")]
    public class RefreshToken
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(256)]
        public string Token { get; set; } = null!; // Para CS8618

        [Required]
        public DateTime Expires { get; set; }

        [Required]
        public DateTime Created { get; set; }

        public DateTime? Revoked { get; set; } // Permite nulo

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; } = null!; // Para CS8618

        // --- ADICIONE ESTA PROPRIEDADE ---
        // Propriedade calculada que não é mapeada para o banco (não precisa de coluna)
        // Retorna true se o token NÃO foi revogado E a data atual é ANTES da expiração.
        [NotMapped] // Importante: informa ao EF para NÃO criar uma coluna para isso
        public bool IsActive => Revoked == null && DateTime.UtcNow < Expires;
        // ---------------------------------

        // Opcional: propriedade para checar apenas expiração
        // [NotMapped]
        // public bool IsExpired => DateTime.UtcNow >= Expires;
    }
}