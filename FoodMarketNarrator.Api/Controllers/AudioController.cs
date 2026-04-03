using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AudioController : ControllerBase
    {
        private readonly AudioService _audioService;
        private readonly IWebHostEnvironment _environment;

        public AudioController(AudioService audioService, IWebHostEnvironment environment)
        {
            _audioService = audioService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _audioService.GetAllAudiosAsync();
            return Ok(data);
        }

        [HttpGet("/Restaurant/{restaurantId}/audios")]
        public async Task<IActionResult> GetByRestaurant(string restaurantId)
        {
            var data = await _audioService.GetByRestaurantIdAsync(restaurantId);
            return Ok(data);
        }

        [HttpGet("/public/audios/{audioId:int}/file")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileByAudioId(int audioId)
        {
            var audio = await _audioService.GetByIdAsync(audioId);
            if (audio == null || string.IsNullOrWhiteSpace(audio.AudioUrl))
            {
                return NotFound(new { message = "Audio not found." });
            }

            var resolvedPath = ResolveAudioFilePath(audio.AudioUrl, audio.LanguageCode);
            if (resolvedPath != null && System.IO.File.Exists(resolvedPath))
            {
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(resolvedPath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return PhysicalFile(resolvedPath, contentType, enableRangeProcessing: true);
            }

            if (Uri.TryCreate(audio.AudioUrl, UriKind.Absolute, out var absoluteUri)
                && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return Redirect(audio.AudioUrl);
            }

            return NotFound(new { message = "Audio file not found." });
        }

        [HttpPost("/Restaurant/{restaurantId}/audios")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Upload(
            string restaurantId,
            [FromForm(Name = "language_id")] int languageId,
            [FromForm(Name = "file")] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required." });
            }

            string webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            string uploadDir = Path.Combine(webRoot, "uploads", "audios");
            Directory.CreateDirectory(uploadDir);

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid():N}{extension}";
            string fullPath = Path.Combine(uploadDir, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            string audioUrl = $"/uploads/audios/{fileName}";
            var created = await _audioService.CreateAsync(restaurantId, languageId, audioUrl);
            return Ok(created);
        }

        [HttpPatch("/Audios/{audioId:int}/active")]
        public async Task<IActionResult> UpdateActive(int audioId, [FromBody] UpdateAudioActiveRequest request)
        {
            bool updated = await _audioService.UpdateActiveAsync(audioId, request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "Audio not found." });
            }

            return Ok(new { message = "Audio active status updated." });
        }

        [HttpDelete("/Audios/{audioId:int}")]
        public async Task<IActionResult> Delete(int audioId)
        {
            bool deleted = await _audioService.DeleteAsync(audioId);
            if (!deleted)
            {
                return NotFound(new { message = "Audio not found." });
            }

            return Ok(new { message = "Audio deleted successfully." });
        }

        private string? ResolveAudioFilePath(string audioUrl, string? languageCode)
        {
            var normalizedUrl = NormalizeAudioUrl(audioUrl);
            if (string.IsNullOrWhiteSpace(normalizedUrl))
            {
                return null;
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var uploadAudioRoot = Path.Combine(webRoot, "uploads", "audios");
            var mauiAudioRoot = Path.GetFullPath(Path.Combine(
                _environment.ContentRootPath,
                "..",
                "FoodMarketNarrator.Maui",
                "Resources",
                "Narration",
                "audio"));

            if (TryResolvePrefixedPath(normalizedUrl, "/uploads/audios/", uploadAudioRoot, out var uploadedAudioPath))
            {
                return uploadedAudioPath;
            }

            if (TryResolvePrefixedPath(normalizedUrl, "/maui-audios/", mauiAudioRoot, out var mauiAudioPath))
            {
                return mauiAudioPath;
            }

            if (TryResolvePrefixedPath(normalizedUrl, "/audio/", mauiAudioRoot, out var legacyAudioPath))
            {
                return legacyAudioPath;
            }

            var fileName = Path.GetFileName(normalizedUrl);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                var languageSpecificPath = Path.Combine(mauiAudioRoot, "languages", languageCode, fileName);
                if (System.IO.File.Exists(languageSpecificPath))
                {
                    return languageSpecificPath;
                }
            }

            var directUploadPath = Path.Combine(uploadAudioRoot, fileName);
            if (System.IO.File.Exists(directUploadPath))
            {
                return directUploadPath;
            }

            var directMauiPath = Path.Combine(mauiAudioRoot, fileName);
            if (System.IO.File.Exists(directMauiPath))
            {
                return directMauiPath;
            }

            var recursiveMatch = Directory.Exists(mauiAudioRoot)
                ? Directory
                    .EnumerateFiles(mauiAudioRoot, fileName, SearchOption.AllDirectories)
                    .FirstOrDefault()
                : null;

            if (!string.IsNullOrWhiteSpace(recursiveMatch))
            {
                return recursiveMatch;
            }

            recursiveMatch = Directory.Exists(uploadAudioRoot)
                ? Directory
                    .EnumerateFiles(uploadAudioRoot, fileName, SearchOption.AllDirectories)
                    .FirstOrDefault()
                : null;

            if (!string.IsNullOrWhiteSpace(recursiveMatch))
            {
                return recursiveMatch;
            }

            return null;
        }

        private static string NormalizeAudioUrl(string audioUrl)
        {
            return audioUrl.Replace("\\", "/", StringComparison.Ordinal).Trim();
        }

        private static bool TryResolvePrefixedPath(string normalizedUrl, string prefix, string rootPath, out string? resolvedPath)
        {
            resolvedPath = null;

            if (!normalizedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = normalizedUrl[prefix.Length..].TrimStart('/');
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            var candidate = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(candidate))
            {
                return false;
            }

            resolvedPath = candidate;
            return true;
        }
    }
}