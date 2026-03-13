using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly LanguageService _languageService;

    public LanguageController(LanguageService languageService)
    {
        _languageService = languageService;
    }

    [HttpGet("{languageCode}")]
    public async Task<IActionResult> GetLanguageByCode(string languageCode)
    {
        var data = await _languageService.GetLanguageByCodeAsync(languageCode);
        
        if (data == null)
            return NotFound();

        return Ok(data);
    }
}