using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/maps")]
[Authorize]
public class MapsController : ControllerBase
{
    private static readonly Regex[] CoordinatePatterns =
    [
        new Regex("@(-?\\d+(?:\\.\\d+)?),\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex("!3d(-?\\d+(?:\\.\\d+)?)!4d(-?\\d+(?:\\.\\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex("[?&]q=(-?\\d+(?:\\.\\d+)?),\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex("[?&]ll=(-?\\d+(?:\\.\\d+)?),\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    ];

    private readonly IHttpClientFactory _httpClientFactory;

    public MapsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("resolve-coordinates")]
    public async Task<IActionResult> ResolveCoordinates([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { message = "url is required." });
        }

        if (!TryParseCoordinates(url, out var directLatitude, out var directLongitude))
        {
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var sourceUri))
            {
                return BadRequest(new { message = "URL Google Maps khong hop le." });
            }

            if (!IsSupportedGoogleMapsHost(sourceUri.Host))
            {
                return BadRequest(new { message = "Chi ho tro link Google Maps." });
            }

            Uri finalUri;
            try
            {
                finalUri = await ResolveFinalUriAsync(sourceUri);
            }
            catch
            {
                return BadRequest(new { message = "Khong the doc thong tin tu link Google Maps." });
            }

            if (!TryParseCoordinates(finalUri.ToString(), out directLatitude, out directLongitude))
            {
                return BadRequest(new { message = "Khong tim thay toa do trong link Google Maps." });
            }
        }

        return Ok(new
        {
            latitude = directLatitude,
            longitude = directLongitude
        });
    }

    private async Task<Uri> ResolveFinalUriAsync(Uri sourceUri)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(8);

        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUri);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        var finalUri = response.RequestMessage?.RequestUri ?? sourceUri;

        if (!IsSupportedGoogleMapsHost(finalUri.Host))
        {
            throw new InvalidOperationException("Unsupported final host.");
        }

        return finalUri;
    }

    private static bool TryParseCoordinates(string raw, out double latitude, out double longitude)
    {
        var decoded = DecodeSafe(raw);

        foreach (var pattern in CoordinatePatterns)
        {
            var match = pattern.Match(decoded);
            if (!match.Success) continue;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) ||
                !double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
            {
                continue;
            }

            if (IsValidCoordinates(latitude, longitude))
            {
                return true;
            }
        }

        latitude = 0;
        longitude = 0;
        return false;
    }

    private static string DecodeSafe(string raw)
    {
        try
        {
            return Uri.UnescapeDataString(raw);
        }
        catch
        {
            return raw;
        }
    }

    private static bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180;
    }

    private static bool IsSupportedGoogleMapsHost(string host)
    {
        var normalized = host.Trim().ToLowerInvariant();
        return normalized is "maps.app.goo.gl"
            or "goo.gl"
            or "www.google.com"
            or "google.com"
            or "maps.google.com";
    }
}
