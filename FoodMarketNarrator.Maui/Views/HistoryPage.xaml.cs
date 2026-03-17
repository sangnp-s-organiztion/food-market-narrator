using food_market_narrator.Services;
using food_market_narrator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace food_market_narrator.Views;

public partial class HistoryPage : ContentPage
{
    private readonly IPOIService? _poiService;
    private readonly IHistoryService? _historyService;

    public HistoryPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _poiService = services?.GetService<IPOIService>();
        _historyService = services?.GetService<IHistoryService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        if (_poiService == null || _historyService == null)
            return;

        var historyIds = _historyService.GetHistory();
        var allPois = await _poiService.GetAllPOIsAsync();

        // Lọc chỉ lấy các POI trong lịch sử (giữ nguyên thứ tự)
        var historyPois = historyIds
            .Select(id => allPois.FirstOrDefault(p => p.restaurantId == id))
            .Where(p => p != null)
            .Cast<POI>()
            .ToList();

        HistoryListContainer.Clear();

        if (historyPois.Count == 0)
        {
            EmptyState.IsVisible = true;
            ListContainer.IsVisible = false;
            return;
        }

        EmptyState.IsVisible = false;
        ListContainer.IsVisible = true;

        foreach (var poi in historyPois)
        {
            var item = CreatePoiCard(poi);
            HistoryListContainer.Add(item);
        }
    }

    private View CreatePoiCard(POI poi)
    {
        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            StrokeThickness = 0,
            Padding = 12,
            Margin = new Thickness(0, 5)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        // Ảnh
        var image = new Image
        {
            Source = poi.PrimaryImage,
            Aspect = Aspect.AspectFill,
            WidthRequest = 80,
            HeightRequest = 80
        };
        image.Clip = new EllipseGeometry
        {
            RadiusX = 40,
            RadiusY = 40,
            Center = new Point(40, 40)
        };

        // Thông tin
        var infoStack = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        infoStack.Add(new Label
        {
            Text = poi.Name ?? "Tên quán",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A1A")
        });

        infoStack.Add(new Label
        {
            Text = poi.Address ?? "Đang cập nhật địa chỉ",
            FontSize = 12,
            TextColor = Color.FromArgb("#757575"),
            MaxLines = 2,
            LineBreakMode = LineBreakMode.WordWrap
        });

        infoStack.Add(new Label
        {
            Text = poi.StatusText,
            FontSize = 11,
            TextColor = Color.FromArgb("#4CAF50")
        });

        grid.Children.Add(image);
        grid.Children.Add(infoStack);

        Grid.SetColumn(image, 0);
        Grid.SetColumn(infoStack, 1);

        // Tap to view detail
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Shell.Current.GoToAsync($"POIDetailPage?restaurantId={Uri.EscapeDataString(poi.restaurantId)}");
        };
        border.GestureRecognizers.Add(tapGesture);

        border.Content = grid;
        return border;
    }

    private async void OnHomeTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
