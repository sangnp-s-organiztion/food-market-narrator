namespace food_market_narrator_api.DTOs.Analytics;

public class ListensTimeseriesResponse
{
    public List<ListenCountItem> Items { get; set; } = [];
}

public class ListenCountItem
{
    public string Date { get; set; } = string.Empty;
    public int Listens { get; set; }
}
