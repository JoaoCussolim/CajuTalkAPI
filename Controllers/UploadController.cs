using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Adicionado para logging
using Microsoft.AspNetCore.Authorization; // Adicionado para proteger a rota de exclusão

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<UploadController> _logger; // Adicionado

        public UploadController(IWebHostEnvironment webHostEnvironment, ILogger<UploadController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger; // Adicionado
        }

        // ROTA POST PARA UPLOAD (EXISTENTE)
        [HttpPost("file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado.");
            }

            var uploadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}";

            return Ok(new { url = fileUrl });
        }

        // **** NOVA ROTA DELETE ****
        [HttpDelete("{fileName}")]
        [Authorize] // Protege a rota, apenas usuários autenticados podem deletar.
        public IActionResult DeleteFile(string fileName)
        {
            // Medida de segurança básica para evitar ataques de "path traversal"
            // Impede que o cliente tente apagar arquivos fora da pasta de uploads.
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                _logger.LogWarning("Tentativa de exclusão de arquivo com nome inválido: {FileName}", fileName);
                return BadRequest(new { message = "Nome de arquivo inválido." });
            }

            try
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);

                _logger.LogInformation("Tentando deletar o arquivo em: {FilePath}", filePath);

                if (System.IO.File.Exists(filePath))
                {
                    // Não permitir a exclusão da foto de perfil padrão
                    if (fileName.Equals("default-profile.png", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Tentativa de deletar a imagem de perfil padrão, operação negada.");
                        return Forbid("A imagem de perfil padrão não pode ser excluída.");
                    }

                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Arquivo deletado com sucesso: {FilePath}", filePath);
                    return Ok(new { message = "Arquivo deletado com sucesso." });
                }
                else
                {
                    _logger.LogWarning("Arquivo não encontrado para exclusão: {FilePath}", filePath);
                    return NotFound(new { message = "Arquivo não encontrado." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar deletar o arquivo: {FileName}", fileName);
                return StatusCode(500, new { message = "Ocorreu um erro interno no servidor ao tentar deletar o arquivo." });
            }
        }
    }
}