using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly RestaurantService _restaurantService;
        private readonly IWebHostEnvironment _environment;

        public ImagesController(RestaurantService restaurantService, IWebHostEnvironment environment)
        {
            _restaurantService = restaurantService;
            _environment = environment;
        }

        [HttpGet("/Restaurant/{restaurantId}/images")]
        public async Task<IActionResult> GetByRestaurant(string restaurantId)
        {
            var images = await _restaurantService.GetImagesByRestaurantIdAsync(restaurantId);
            return Ok(images);
        }

        [HttpPost("/Restaurant/{restaurantId}/images")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Upload(
            string restaurantId,
            [FromForm(Name = "file")] IFormFile file,
            [FromForm(Name = "is_primary")] bool isPrimary = false,
            [FromForm(Name = "sort_order")] int sortOrder = 0)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required." });
            }

            string uploadDir = Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    "..",
                    "FoodMarketNarrator.Maui",
                    "Resources",
                    "Images"));
            Directory.CreateDirectory(uploadDir);

            string fileName = BuildImageFileName(file.FileName);
            string fullPath = Path.Combine(uploadDir, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            string imageUrl = $"/maui-images/{fileName}";
            var created = await _restaurantService.AddImageAsync(restaurantId, imageUrl, isPrimary, sortOrder);
            return Ok(created);
        }

        private static string BuildImageFileName(string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            extension = extension.ToLowerInvariant();
            string baseName = Path.GetFileNameWithoutExtension(originalFileName).Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "upload";
            }

            // Keep file names compatible with MAUI image resource loading:
            // lowercase ASCII and underscores only.
            var normalized = baseName.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    builder.Append('_');
                }
            }

            string sanitizedName = builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Trim('_');

            while (sanitizedName.Contains("__"))
            {
                sanitizedName = sanitizedName.Replace("__", "_");
            }

            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "upload";
            }

            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            return $"img_{sanitizedName}_{uniqueSuffix}{extension}";
        }

        [HttpDelete("/Images/{imageId:int}")]
        public async Task<IActionResult> Delete(int imageId)
        {
            var existing = await _restaurantService.GetImageByIdAsync(imageId);
            if (existing == null)
            {
                return NotFound(new { message = "Image not found." });
            }

            bool deleted = await _restaurantService.DeleteImageAsync(imageId);
            if (!deleted)
            {
                return NotFound(new { message = "Image not found." });
            }

            DeletePhysicalImage(existing.ImageUrl);

            return Ok(new { message = "Image deleted successfully." });
        }

        private void DeletePhysicalImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            string fileName = Path.GetFileName(imageUrl.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            string uploadDir = Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    "..",
                    "FoodMarketNarrator.Maui",
                    "Resources",
                    "Images"));

            string fullPath = Path.Combine(uploadDir, fileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        [HttpPatch("/Images/{imageId:int}/primary")]
        public async Task<IActionResult> SetPrimary(int imageId, [FromBody] SetPrimaryImageRequest request)
        {
            bool updated = await _restaurantService.SetPrimaryImageAsync(imageId, request.IsPrimary);
            if (!updated)
            {
                return NotFound(new { message = "Image not found." });
            }

            return Ok(new { message = "Image primary status updated." });
        }

        [HttpPatch("/Restaurant/{restaurantId}/images/reorder")]
        public async Task<IActionResult> Reorder(string restaurantId, [FromBody] ReorderImagesRequest request)
        {
            bool updated = await _restaurantService.ReorderImagesAsync(restaurantId, request.Items);
            if (!updated)
            {
                return NotFound(new { message = "Restaurant or images not found." });
            }

            return Ok(new { message = "Images reordered successfully." });
        }
    }
}
