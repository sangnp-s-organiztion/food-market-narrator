namespace food_market_narrator_api.Authorization;

public static class PublicEndpoints
{
    // Khai bao tat ca endpoint public tai day de de maintain.
    public static readonly IReadOnlyList<PublicEndpointDefinition> Definitions =
    [
        new("POST", "/Auth/login"),
        new("GET", "/Language"),
        new("GET", "/Language/{languageCode}"),
        new("GET", "/Restaurant"),
        new("GET", "/Restaurant/{id}"),
        new("GET", "/audio"),
        new("GET", "/Restaurant/{restaurantId}/dishes"),
        new("GET", "/Restaurant/{restaurantId}/images"),
        new("GET", "/Restaurant/{restaurantId}/audios")
    ];
}
