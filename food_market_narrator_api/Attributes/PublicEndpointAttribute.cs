using Microsoft.AspNetCore.Authorization;

namespace food_market_narrator_api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PublicEndpointAttribute : AllowAnonymousAttribute
{
}
