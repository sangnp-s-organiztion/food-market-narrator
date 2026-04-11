using System.Security.Claims;
using food_market_narrator_api.Authorization;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/translation-billing")]
[Authorize(AuthenticationSchemes = AuthSchemes.Saler)]
public class TranslationBillingController : ControllerBase
{
    private readonly AdminTranslationBillingService _translationBillingService;

    public TranslationBillingController(AdminTranslationBillingService translationBillingService)
    {
        _translationBillingService = translationBillingService;
    }

    [HttpGet("my-usage")]
    public async Task<IActionResult> GetMyUsageLedger(
        [FromQuery] string? billingMonth = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var salerIdentity = User.Identities.FirstOrDefault(identity =>
            identity.HasClaim(ClaimTypes.Role, "saler"));

        var currentUserIdRaw = salerIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(currentUserIdRaw, out var currentUserId) || currentUserId <= 0)
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        try
        {
            var result = await _translationBillingService.GetUsageLedgerBySellerUserIdAsync(
                billingMonth,
                currentUserId,
                page,
                pageSize);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

