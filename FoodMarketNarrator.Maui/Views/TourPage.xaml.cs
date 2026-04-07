using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

public partial class TourPage : ContentPage
{
    private readonly ITourService? _tourService;
    private readonly Dictionary<int, TourModel> _tourMap = new();

    public TourPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _tourService = services?.GetService<ITourService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadToursAsync();
    }

    private async Task LoadToursAsync()
    {
        if (_tourService == null)
        {
            TourCollectionView.ItemsSource = new List<TourModel>();
            return;
        }

        var tours = await _tourService.GetToursAsync();
        TourCollectionView.ItemsSource = tours;

        _tourMap.Clear();
        foreach (var tour in tours)
        {
            _tourMap[tour.TourId] = tour;
        }
    }

    private async void OnStartTourClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.CommandParameter is int tourId && _tourMap.TryGetValue(tourId, out var tour))
        {
            var poiIds = tour.Stops
                .Select(s => s.RestaurantId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (poiIds.Count > 0)
            {
                var encodedPoiIds = Uri.EscapeDataString(string.Join(',', poiIds));
                var encodedTourName = Uri.EscapeDataString(tour.Name ?? string.Empty);
                await Shell.Current.GoToAsync($"//MapPage?tourPoiIds={encodedPoiIds}&tourName={encodedTourName}");
                return;
            }
        }

        await Shell.Current.GoToAsync("//MapPage");
    }
}
