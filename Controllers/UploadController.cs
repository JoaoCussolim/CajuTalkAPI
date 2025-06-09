using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq; // Adicionado para usar o LINQ
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace TWTodos.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment webHostEnvironment, ILogger<UploadController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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

        // **** NOVA ROTA GET PARA LISTAR ARQUIVOS ****
        [HttpGet("files")]
        public IActionResult GetFiles()
        {
            try
            {
                var uploadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

                if (!Directory.Exists(uploadsFolderPath))
                {
                    // Se a pasta não existe, retorna uma lista vazia.
                    _logger.LogInformation("O diretório de uploads não foi encontrado. Retornando lista vazia.");
                    return Ok(new string[0]);
                }

                // Obtém todos os caminhos de arquivo no diretório
                var filePaths = Directory.GetFiles(uploadsFolderPath);

                // Mapeia os caminhos completos para apenas os nomes dos arquivos
                var fileNames = filePaths.Select(filePath => Path.GetFileName(filePath)).ToList();

                // Opcional: Se quiser retornar as URLs completas em vez de apenas os nomes
                var fileUrls = fileNames.Select(fileName => $"{Request.Scheme}://{Request.Host}/uploads/{fileName}").ToList();

                _logger.LogInformation("Retornando {Count} arquivos do diretório de uploads.", fileUrls.Count);

                // Retorna a lista de URLs
                return Ok(fileUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar listar os arquivos.");
                return StatusCode(500, new { message = "Ocorreu um erro interno no servidor ao listar os arquivos." });
            }
        }

        // ROTA DELETE (EXISTENTE)
        [HttpDelete("{fileName}")]
        [Authorize]
        public IActionResult DeleteFile(string fileName)
        {
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