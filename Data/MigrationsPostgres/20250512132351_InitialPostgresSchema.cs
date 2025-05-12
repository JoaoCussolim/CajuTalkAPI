using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CajuTalkAPI.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CajuTalk");

            migrationBuilder.CreateTable(
                name: "Mensagem",
                schema: "CajuTalk",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_Sala = table.Column<int>(type: "integer", nullable: false),
                    ID_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoMensagem = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensagem", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SalaChat",
                schema: "CajuTalk",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Publica = table.Column<bool>(type: "boolean", nullable: false),
                    Senha = table.Column<string>(type: "text", nullable: true),
                    FotoPerfilURL = table.Column<string>(type: "text", nullable: true),
                    CriadorID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaChat", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                schema: "CajuTalk",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    LoginUsuario = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    FotoPerfilURL = table.Column<string>(type: "text", nullable: true),
                    CorFundo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Usuario_Sala",
                schema: "CajuTalk",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_Usuario = table.Column<int>(type: "integer", nullable: false),
                    ID_Sala = table.Column<int>(type: "integer", nullable: false),
                    Criador = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioBanido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario_Sala", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "CajuTalk",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RefreshToken_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "CajuTalk",
                        principalTable: "Usuario",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Token",
                schema: "CajuTalk",
                table: "RefreshToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UsuarioId",
                schema: "CajuTalk",
                table: "RefreshToken",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Sala_ID_Usuario_ID_Sala",
                schema: "CajuTalk",
                table: "Usuario_Sala",
                columns: new[] { "ID_Usuario", "ID_Sala" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mensagem",
                schema: "CajuTalk");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "CajuTalk");

            migrationBuilder.DropTable(
                name: "SalaChat",
                schema: "CajuTalk");

            migrationBuilder.DropTable(
                name: "Usuario_Sala",
                schema: "CajuTalk");

            migrationBuilder.DropTable(
                name: "Usuario",
                schema: "CajuTalk");
        }
    }
}
