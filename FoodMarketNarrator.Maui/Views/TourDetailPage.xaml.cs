using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

[QueryProperty(nameof(TourId), "tourId")]
public partial class TourDetailPage : ContentPage
{
    private readonly ITourService? _tourService;
    private int _tourId;
    private TourModel? _currentTour;
    private bool _isLoading;
    private int _loadedTourId;
    private bool _pendingLoadOnAppear;

    public string TourId
    {
        get => _tourId.ToString();
        set
        {
            if (!int.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var parsedTourId) || parsedTourId <= 0)
            {
                _tourId = 0;
                _loadedTourId = 0;
                _currentTour = null;
                _pendingLoadOnAppear = false;
                _ = MainThread.InvokeOnMainThreadAsync(() => SetVisualState(isLoading: false, isError: true, hasData: false));
                return;
            }

            if (parsedTourId == _tourId && _currentTour != null && _loadedTourId == parsedTourId)
            {
                return;
            }

            _tourId = parsedTourId;
            _currentTour = null;
            _loadedTourId = 0;
            _pendingLoadOnAppear = true;
        }
    }

    public TourDetailPage()
    {
        InitializeComponent();
        _tourService = Application.Current?.Handler?.MauiContext?.Services?.GetService<ITourService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isLoading && _tourId > 0 && (_pendingLoadOnAppear || _currentTour == null))
        {
            // Let Shell finish transition first so users see destination loading UI immediately.
            await Task.Yield();
            _pendingLoadOnAppear = false;
            await LoadTourDetailAsync();
        }
    }

    private async Task LoadTourDetailAsync()
    {
        if (_tourService is null || _tourId <= 0)
        {
            SetVisualState(isLoading: false, isError: true, hasData: false);
            return;
        }

        if (_isLoading)
        {
            return;
        }

        if (_currentTour != null && _loadedTourId == _tourId)
        {
            SetVisualState(isLoading: false, isError: false, hasData: true);
            return;
        }

        _isLoading = true;
        var requestedTourId = _tourId;
        SetVisualState(isLoading: true, isError: false, hasData: false);

        try
        {
            var tour = await _tourService.GetTourByIdAsync(requestedTourId);

            if (requestedTourId != _tourId)
            {
                return;
            }

            if (tour == null)
            {
                _currentTour = null;
                _loadedTourId = 0;
                SetVisualState(isLoading: false, isError: true, hasData: false);
                return;
            }

            tour.Stops = tour.Stops
                .OrderBy(s => s.StopOrder)
                .ThenBy(s => s.RestaurantId, StringComparer.Ordinal)
                .ToList();

            foreach (var stop in tour.Stops)
            {
                if (string.IsNullOrWhiteSpace(stop.PrimaryImageUrl))
                {
                    stop.PrimaryImageUrl = "dotnet_bot.svg";
                }

                if (string.IsNullOrWhiteSpace(stop.Address))
                {
                    stop.Address = "Đang cập nhật địa chỉ";
                }
            }

            _currentTour = tour;
            _loadedTourId = requestedTourId;
            BindingContext = tour;

            DurationMetricValue.Text = tour.EstimatedDurationMinutes.HasValue
                ? $"{tour.EstimatedDurationMinutes.Value} phút"
                : "--";
            StopMetricValue.Text = $"{Math.Max(tour.StopCount, tour.Stops.Count)} điểm dừng";

            TourDescriptionLabel.Text = !string.IsNullOrWhiteSpace(tour.Description)
                ? tour.Description
                : !string.IsNullOrWhiteSpace(tour.ShortDescription)
                    ? tour.ShortDescription
                    : "Hành trình này chưa có mô tả chi tiết.";

            StartJourneyButton.IsEnabled = tour.Stops.Count > 0;
            StartJourneyButton.Opacity = tour.Stops.Count > 0 ? 1 : 0.6;

            SetVisualState(isLoading: false, isError: false, hasData: true);
        }
        catch
        {
            _currentTour = null;
            _loadedTourId = 0;
            SetVisualState(isLoading: false, isError: true, hasData: false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SetVisualState(bool isLoading, bool isError, bool hasData)
    {
        LoadingContainer.IsVisible = isLoading;
        ErrorContainer.IsVisible = isError;
        ContentContainer.IsVisible = hasData;
        BottomActionContainer.IsVisible = hasData;
    }

    // Giữ logic back giống POIDetailPage để hành vi nhất quán.
    private async void OnBackButtonTapped(object sender, EventArgs e)
    {
        var navigation = Shell.Current?.Navigation;
        if (navigation?.NavigationStack != null && navigation.NavigationStack.Count > 1)
        {
            await navigation.PopAsync(false);
            return;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        await LoadTourDetailAsync();
    }

    private async void OnViewMapTapped(object sender, EventArgs e)
    {
        await NavigateTourToMapAsync(_currentTour);
    }

    private async void OnStartJourneyClicked(object sender, EventArgs e)
    {
        await NavigateTourToMapAsync(_currentTour);
    }

    private async void OnStopSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StopsCollectionView.SelectedItem is not TourStopModel selectedStop)
        {
            return;
        }

        StopsCollectionView.SelectedItem = null;

        if (Shell.Current == null || string.IsNullOrWhiteSpace(selectedStop.RestaurantId))
        {
            return;
        }

        var encodedId = Uri.EscapeDataString(selectedStop.RestaurantId);
        await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?restaurantId={encodedId}");
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
                var orderedStops = (tour.Stops ?? new List<TourStopModel>())
                    .OrderBy(s => s.StopOrder)
                    .ThenBy(s => s.RestaurantId, StringComparer.Ordinal)
                    .ToList();

                var poiIds = orderedStops
                    .Select(s => s.RestaurantId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var stopOrderByPoiId = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var stop in orderedStops)
                {
                    if (string.IsNullOrWhiteSpace(stop.RestaurantId)
                        || stop.StopOrder <= 0
                        || stopOrderByPoiId.ContainsKey(stop.RestaurantId))
                    {
                        continue;
                    }

                    stopOrderByPoiId[stop.RestaurantId] = stop.StopOrder;
                }

                if (poiIds.Count > 0)
                {
                    var encodedPoiIds = Uri.EscapeDataString(string.Join(',', poiIds));
                    var encodedTourName = Uri.EscapeDataString(tour.Name ?? string.Empty);
                    var encodedTourStopOrders = Uri.EscapeDataString(
                        string.Join(',', stopOrderByPoiId.Select(x => $"{x.Key}:{x.Value}")));

                    await Shell.Current.GoToAsync(
                        $"//MapPage?tourPoiIds={encodedPoiIds}&tourName={encodedTourName}&tourStopOrders={encodedTourStopOrders}");
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
