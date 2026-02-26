using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly AudioService _audioService;

        public AudioController(AudioService audioService)
        {
            _audioService = audioService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _audioService.GetAllAudiosAsync();
            return Ok(data);
        }
    }
}