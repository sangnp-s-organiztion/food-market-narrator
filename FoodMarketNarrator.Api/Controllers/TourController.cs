using food_market_narrator_api.Services;
using food_market_narrator_api.DTOs.Tour;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Globalization;
using System.Text;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TourController : ControllerBase
{
    private readonly TourService _tourService;
    private readonly IWebHostEnvironment _environment;

    public TourController(TourService tourService, IWebHostEnvironment environment)
    {
        _tourService = tourService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double radiusMeters = 30)
    {
        if (radiusMeters <= 0)
        {
            return BadRequest(new { message = "radiusMeters must be greater than 0." });
        }

        var data = await _tourService.GetAllToursAsync(latitude, longitude, radiusMeters);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double radiusMeters = 30)
    {
        if (radiusMeters <= 0)
        {
            return BadRequest(new { message = "radiusMeters must be greater than 0." });
        }

        var data = await _tourService.GetTourByIdAsync(id, latitude, longitude, radiusMeters);
        if (data == null)
        {
            return NotFound(new { message = "Tour not found." });
        }

        return Ok(data);
    }

    [HttpPost("{id:int}/restaurants")]
    public async Task<IActionResult> AddRestaurantToTour(int id, [FromBody] AddTourRestaurantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tourService.AddRestaurantToTourAsync(id, request.RestaurantId);

        return result.Status switch
        {
            AddTourRestaurantStatus.Success => Ok(new { message = "Restaurant added to tour." }),
            AddTourRestaurantStatus.NotFound => NotFound(new { message = result.Message }),
            AddTourRestaurantStatus.Conflict => Conflict(new { message = result.Message }),
            AddTourRestaurantStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to add restaurant to tour." })
        };
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> CreateTour([FromForm] CreateTourRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(ModelState);
        }

        int? createdBy = null;
        var createdByRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(createdByRaw, out var parsedCreatedBy))
        {
            createdBy = parsedCreatedBy;
        }

        string? urlImage = null;
        if (request.File != null && request.File.Length > 0)
        {
            urlImage = await SaveImageFileAsync(request.File);
        }

        var result = await _tourService.CreateTourAsync(
            request.Name,
            request.ShortDescription,
            request.Description,
            request.EstimatedDurationMinutes,
            urlImage ?? request.UrlImage,
            request.IsActive,
            createdBy);

        return result.Status switch
        {
            CreateTourStatus.Success => Ok(result.Data),
            CreateTourStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to create tour." })
        };
    }

    [HttpPost("upload-image")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadImage([FromForm(Name = "file")] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        var imageUrl = await SaveImageFileAsync(file);
        return Ok(new { imageUrl });
    }

    [HttpPost("{id:int}/upload-image")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadImageForTour(int id, [FromForm(Name = "file")] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        var imageUrl = await SaveImageFileAsync(file);
        var result = await _tourService.SetTourImageAsync(id, imageUrl);

        return result.Status switch
        {
            UpdateTourStatus.Success => Ok(new { imageUrl, message = "Tour image updated." }),
            UpdateTourStatus.NotFound => NotFound(new { message = result.Message }),
            UpdateTourStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to upload tour image." })
        };
    }

    [HttpPut("{id:int}/stops/order")]
    public async Task<IActionResult> ReorderStops(int id, [FromBody] ReorderTourStopsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tourService.ReorderTourStopsAsync(id, request.RestaurantIds);
        return result.Status switch
        {
            ReorderTourStopsStatus.Success => Ok(new { message = "Tour stop order updated." }),
            ReorderTourStopsStatus.NotFound => NotFound(new { message = result.Message }),
            ReorderTourStopsStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to reorder stops." })
        };
    }

    [HttpPatch("{id:int}")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateTour(int id, [FromForm] UpdateTourRequest request)
    {
        // If a new file is uploaded, save it; otherwise keep the existing UrlImage from DB
        string? newUrlImage = null;
        if (request.File != null && request.File.Length > 0)
        {
            newUrlImage = await SaveImageFileAsync(request.File);
        }

        // NormalizeUrlImageForStorage(null) = null → keeps existing DB value
        // NormalizeUrlImageForStorage("...") = "..." → replaces with new image path
        var urlImageToSave = newUrlImage ?? request.UrlImage;

        var result = await _tourService.UpdateTourAsync(
            id,
            request.Name,
            request.Description,
            request.EstimatedDurationMinutes,
            urlImageToSave,
            request.IsActive);

        return result.Status switch
        {
            UpdateTourStatus.Success => Ok(new { message = "Tour updated." }),
            UpdateTourStatus.NotFound => NotFound(new { message = result.Message }),
            UpdateTourStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to update tour." })
        };
    }

    private async Task<string> SaveImageFileAsync(IFormFile file)
    {
        string uploadDir = Path.GetFullPath(
            Path.Combine(
                _environment.ContentRootPath,
                "..",
                "FoodMarketNarrator.Maui",
                "Resources",
                "Images"));
        Directory.CreateDirectory(uploadDir);

        string fileName = BuildTourImageFileName(file.FileName);
        string fullPath = Path.Combine(uploadDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return $"/maui-images/{fileName}";
    }

    private static string BuildTourImageFileName(string originalFileName)
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
            baseName = "tour";
        }

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
            sanitizedName = "tour";
        }

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        return $"tour_{sanitizedName}_{uniqueSuffix}{extension}";
    }
}
