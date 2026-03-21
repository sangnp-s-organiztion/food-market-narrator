using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}