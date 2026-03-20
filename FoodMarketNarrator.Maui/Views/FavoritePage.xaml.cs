using food_market_narrator.Services;
using food_market_narrator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace food_market_narrator.Views;

public partial class FavoritePage : ContentPage
{
    private readonly IPOIService? _poiService;
    private readonly IFavoriteService? _favoriteService;

    public FavoritePage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _poiService = services?.GetService<IPOIService>();
        _favoriteService = services?.GetService<IFavoriteService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        if (_poiService == null || _favoriteService == null)
            return;

        var favoriteIds = _favoriteService.GetFavorites();
        var allPois = await _poiService.GetAllPOIsAsync();

        // Lọc chỉ lấy các POI yêu thích
        var favoritePois = allPois
            .Where(p => favoriteIds.Contains(p.restaurantId))
            .ToList();

        FavoriteListContainer.Clear();

        if (favoritePois.Count == 0)
        {
            EmptyState.IsVisible = true;
            ListContainer.IsVisible = false;
            return;
        }

        EmptyState.IsVisible = false;
        ListContainer.IsVisible = true;

        foreach (var poi in favoritePois)
        {
            var item = CreatePoiCard(poi);
            FavoriteListContainer.Add(item);
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
            ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Ảnh
        var image = new Image
        {
            Source = poi.PrimaryImage,
            Aspect = Aspect.AspectFill,
            WidthRequest = 80,
            HeightRequest = 80,
            Margin = new Thickness(0, 0, 8, 0)
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

        // Nút xóa yêu thích
        var deleteBtn = new Label
        {
            Text = "\uf2ed", // Heart broken icon
            FontFamily = "FASolid",
            FontSize = 24,
            TextColor = Color.FromArgb("#E57373"),
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        var deleteTap = new TapGestureRecognizer();
        deleteTap.Tapped += async (s, e) =>
        {
            _favoriteService?.RemoveFavorite(poi.restaurantId);
            await LoadFavoritesAsync();
        };
        deleteBtn.GestureRecognizers.Add(deleteTap);

        grid.Children.Add(image);
        grid.Children.Add(infoStack);
        grid.Children.Add(deleteBtn);

        Grid.SetColumn(image, 0);
        Grid.SetColumn(infoStack, 1);
        Grid.SetColumn(deleteBtn, 2);

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
