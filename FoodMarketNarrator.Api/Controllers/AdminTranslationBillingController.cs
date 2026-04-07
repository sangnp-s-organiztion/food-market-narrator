using System.Security.Claims;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/admin/translation-billing")]
[Authorize]
public class AdminTranslationBillingController : ControllerBase
{
    private readonly AdminTranslationBillingService _adminTranslationBillingService;

    public AdminTranslationBillingController(AdminTranslationBillingService adminTranslationBillingService)
    {
        _adminTranslationBillingService = adminTranslationBillingService;
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyBilling(
        [FromQuery] string? billingMonth = null,
        [FromQuery] int? sellerUserId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        try
        {
            var result = await _adminTranslationBillingService.GetMonthlyBillingAsync(
                billingMonth,
                sellerUserId,
                page,
                pageSize);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsageLedger(
        [FromQuery] string? billingMonth = null,
        [FromQuery] int? sellerUserId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        try
        {
            var result = await _adminTranslationBillingService.GetUsageLedgerAsync(
                billingMonth,
                sellerUserId,
                status,
                page,
                pageSize);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsCurrentUserAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    }
}
