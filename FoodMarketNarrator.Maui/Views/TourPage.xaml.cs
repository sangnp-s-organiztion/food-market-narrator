using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

public partial class TourPage : ContentPage
{
    private readonly ITourService? _tourService;
    private readonly TourImageWarmupService? _tourImageWarmupService;
    private readonly Dictionary<int, TourModel> _tourMap = new();

    public TourPage()
        : this(
            Application.Current?.Handler?.MauiContext?.Services?.GetService<ITourService>(),
            Application.Current?.Handler?.MauiContext?.Services?.GetService<TourImageWarmupService>())
    {
    }

    public TourPage(ITourService? tourService, TourImageWarmupService? warmupService)
    {
        InitializeComponent();
        _tourService = tourService;
        _tourImageWarmupService = warmupService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadToursAsync();
        }
        catch
        {
            TourCollectionView.ItemsSource = new List<TourModel>();
        }
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

        // Warmup ảnh chỉ là tối ưu, không được làm crash flow Tour.
        try
        {
            _tourImageWarmupService?.WarmupTourImages(tours);
        }
        catch
        {
            // Ignore warmup failure to keep Tour page stable in release mode.
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
            await NavigateTourToMapAsync(tour);
            return;
        }

        await NavigateTourToMapAsync(null);
    }

    private async void OnTourCardTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        await NavigateTourToMapAsync(border.BindingContext as TourModel);
    }

    private static async Task NavigateTourToMapAsync(TourModel? tour)
    {
        if (Shell.Current == null)
        {
            return;
        }

        try
        {
            if (tour != null)
            {
                var poiIds = (tour.Stops ?? new List<TourStopModel>())
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
        catch
        {
            await Shell.Current.GoToAsync("//MapPage");
        }
    }
}
