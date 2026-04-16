using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("public/translations")]
public class PublicTranslationController : ControllerBase
{
    private readonly UiTranslationService _uiTranslationService;

    public PublicTranslationController(UiTranslationService uiTranslationService)
    {
        _uiTranslationService = uiTranslationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTranslations(
        [FromQuery] string languageCode,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityIds = null)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return BadRequest(new { message = "languageCode is required." });
        }

        var parsedEntityIds = ParseEntityIds(entityIds);

        try
        {
            var data = await _uiTranslationService.GetUiTranslationsAsync(
                languageCode,
                entityType,
                parsedEntityIds);

            return Ok(data);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static List<string> ParseEntityIds(string? entityIds)
    {
        if (string.IsNullOrWhiteSpace(entityIds))
        {
            return new List<string>();
        }

        return entityIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
