using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Mapsui;
using Mapsui.Projections;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    private enum PoiCategoryFilter
    {
        All,
        Nearby,
        Favorite,
        OpenNow
    }

    private static bool _hasAutoStartedNarrationThisSession; // cờ để đảm bảo tự động bắt đầu thuyết minh chỉ 1 lần duy nhất trong mỗi phiên chạy app, tránh việc tự động bật lại khi người dùng quay lại MainPage sau khi đã có tương tác với thuyết minh trong phiên đó
    private static bool _hasAppliedStartupTrackingDelay; // cờ để đảm bảo chỉ áp dụng delay khi bắt đầu tracking lần đầu tiên trong phiên chạy app, tránh delay không cần thiết khi quay lại MainPage sau khi đã có vị trí gần đó
    private static bool? _lastFloatingButtonVisibility; // cache trạng thái visible của FloatingButton để tránh phải tính toán lại khi quay lại MainPage nếu vị trí không thay đổi nhiều
    private const int FeaturedPoiPageSize = 10; // số lượng POI hiển thị trên mỗi trang khi phân trang
    private const double NearbyFilterRadiusMeters = 100; // bán kính để xác định POI nào được coi là "gần" khi áp dụng filter Nearby

    // Khời tạo tọa độ và tên cho điểm
    private readonly IPOIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IFavoriteService _favoriteService;
    private readonly ILanguageService _languageService;

    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại có ở gần POI hay không
    private bool _isMapLoaded;
    private bool _isPoiListBound;
    private List<POI> _allPois = new();
    private int _currentPoiPageIndex; // chỉ số trang hiện tại trong phân trang POI
    private Location? _lastKnownLocation; // cache vị trí gần nhất để có thể cập nhật UI ngay khi quay lại MainPage mà không phải chờ sự kiện LocationChanged nếu vị trí không thay đổi nhiều
    private bool _isInitializingMainPage;
    private PoiCategoryFilter _activePoiFilter = PoiCategoryFilter.All; // filter đang được áp dụng cho danh sách POI, mặc định là All (tất cả)
    private List<POI> _filteredPois = new();
    private string _lastAppliedLanguageCode = string.Empty;
    private bool _isLanguageSelectionPromptVisible;
    private Grid? _firstLaunchLanguagePopupOverlay;
    private VerticalStackLayout? _firstLaunchLanguageOptionsContainer;
    private readonly Dictionary<string, Border> _firstLaunchLanguageOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _firstLaunchLanguageChecks = new(StringComparer.OrdinalIgnoreCase);

	// Hàm khởi tạo MainPage mới
    public MainPage(
        IPOIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService,
        IAudioLibraryService audioLibraryService,
        IFavoriteService favoriteService,
        ILanguageService languageService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _audioLibraryService = audioLibraryService;
        _favoriteService = favoriteService;
        _languageService = languageService;
    }

    // khôi phục trạng thái + cập nhật theo location + load dữ liệu nền + xử lý audio khi page xuất hiện
    protected override void OnAppearing()
    {
        base.OnAppearing();
        var sw = Stopwatch.StartNew();

        // Khôi phục trạng thái visible của FloatingButton dựa trên cache nếu có, tránh việc phải tính toán lại khoảng cách đến POI và cập nhật UI nếu vị trí không thay đổi nhiều kể từ lần trước khi rời khỏi MainPage
        if (_lastFloatingButtonVisibility.HasValue)
        {
            var cachedVisibility = _lastFloatingButtonVisibility.Value;
            _isInsidePOIUI = cachedVisibility;
            FloatingButton.IsVisible = cachedVisibility;
        }

        // Đăng ký sự kiện thay đổi vị trí để cập nhật UI và thuyết minh khi có vị trí mới, đăng ký ở đây để đảm bảo luôn có sự kiện được xử lý khi ở trên MainPage, kể cả khi người dùng quay lại từ trang khác mà không có sự kiện LocationChanged mới (ví dụ khi quay lại từ Settings sau khi bật/tắt thuyết minh)
        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        LogPerf("OnAppearing: subscribed LocationChanged", sw);

        if (!string.Equals(_lastAppliedLanguageCode, _languageService.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _ = RefreshPoiListForCurrentLanguageAsync();
        }

        // Dời start tracking sau frame đầu để giảm giật lúc cold start.
        _ = StartTrackingDeferredAsync();

        // Ưu tiên dùng vị trí cache để cập nhật nút thuyết minh ngay khi quay lại MainPage.
        var currentLocation = _locationService.LastKnownLocation ?? _lastKnownLocation;
        if (currentLocation != null)
        {
            _lastKnownLocation = currentLocation;
            UpdateUIByLocation(currentLocation);
            _ = EnsurePoiDataReadyForUiAsync(currentLocation);
        }
        else
        {
            _ = PrimeUiWithLatestLocationAsync();
        }

        // Trả giao diện ngay, các phần nặng sẽ được tải nền.
        if (!_isInitializingMainPage)
        {
            _ = InitializeMainPageAsync();
        }

        // Chỉ tự bật 1 lần trong mỗi phiên chạy app (cold start).
        if (!_hasAutoStartedNarrationThisSession)
        {
            _narrationFlowService.StartNarration();
            _hasAutoStartedNarrationThisSession = true;
        }

        if (_audioLibraryService.ConsumeStartupOfflineNoticeFlag())
        {
            _ = DisplayAlert(
                LocalizationResourceManager.Instance["NoticeTitle"],
                LocalizationResourceManager.Instance["AudioInternetRequiredMessage"],
                LocalizationResourceManager.Instance["Close"]);
        }

        // Cập nhật text/disabled state của nút, trạng thái visible đã được quyết định ở nhánh trên.
        UpdateFloatingButtonUI();

        if (_isPoiListBound)
        {
            ApplyPoiFilterAndRefresh();
        }

        _ = EnsureFirstLaunchLanguageSelectionAsync();

        LogPerf("OnAppearing: completed", sw);
    }

    private async Task EnsureFirstLaunchLanguageSelectionAsync()
    {
        if (_isLanguageSelectionPromptVisible)
        {
            return;
        }

        if (Preferences.Get("language_selected", false))
        {
            return;
        }

        _isLanguageSelectionPromptVisible = true;

        try
        {
            var currentLanguageCode = CanonicalizeLanguageCode(_languageService.CurrentLanguage);
            var languageOptions = NormalizeLanguageOptions(await _languageService.GetAllLanguagesAsync(), currentLanguageCode);
            await ShowFirstLaunchLanguagePopupAsync(languageOptions, currentLanguageCode);
        }
        catch
        {
            // Keep app usable when language picker cannot be shown.
            _isLanguageSelectionPromptVisible = false;
        }
    }

    private async Task ShowFirstLaunchLanguagePopupAsync(
        IReadOnlyCollection<LanguageModel> languageOptions,
        string currentLanguageCode)
    {
        if (_firstLaunchLanguagePopupOverlay != null)
        {
            return;
        }

        var options = languageOptions
            .Where(x => !string.IsNullOrWhiteSpace(x.LanguageCode))
            .Select(x => new LanguageModel
            {
                LanguageCode = CanonicalizeLanguageCode(x.LanguageCode),
                LanguageName = x.LanguageName
            })
            .GroupBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

        if (options.Count == 0)
        {
            _isLanguageSelectionPromptVisible = false;
            return;
        }

        _firstLaunchLanguagePopupOverlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#4D000000"),
            InputTransparent = false,
            Opacity = 0
        };

        var dismissBackdrop = new BoxView
        {
            Color = Colors.Transparent,
            InputTransparent = false
        };
        dismissBackdrop.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await CloseFirstLaunchLanguagePopupAsync())
        });
        _firstLaunchLanguagePopupOverlay.Children.Add(dismissBackdrop);

        var popupContent = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 32 },
            Padding = new Thickness(20, 14, 20, 24),
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, -2)
        };

        var contentStack = new VerticalStackLayout { Spacing = 16 };

        var titleGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var titleLabel = new Label
        {
            Text = LocalizationResourceManager.Instance["SelectNarrationLanguage"],
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A1A"),
            VerticalOptions = LayoutOptions.Center
        };
        titleGrid.Add(titleLabel, 0, 0);

        var closeButton = new Border
        {
            BackgroundColor = Color.FromArgb("#E8E4E0"),
            HeightRequest = 56,
            WidthRequest = 56,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            HorizontalOptions = LayoutOptions.End
        };
        closeButton.Content = new Label
        {
            Text = "\uF00D",
            FontFamily = "FASolid",
            FontSize = 20,
            TextColor = Color.FromArgb("#6F6862"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        closeButton.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await CloseFirstLaunchLanguagePopupAsync())
        });
        titleGrid.Add(closeButton, 1, 0);
        contentStack.Add(titleGrid);

        _firstLaunchLanguageOptionsContainer = new VerticalStackLayout { Spacing = 10 };
        contentStack.Add(_firstLaunchLanguageOptionsContainer);

        popupContent.Content = contentStack;

        var mainStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.End };
        mainStack.Add(popupContent);
        _firstLaunchLanguagePopupOverlay.Children.Add(mainStack);

        if (Content is not Grid mainGrid)
        {
            _isLanguageSelectionPromptVisible = false;
            _firstLaunchLanguagePopupOverlay = null;
            return;
        }

        Grid.SetRowSpan(_firstLaunchLanguagePopupOverlay, 2);
        mainGrid.Children.Add(_firstLaunchLanguagePopupOverlay);

        BuildFirstLaunchLanguageOptions(options);
        ApplyFirstLaunchLanguageSelectionStyle(currentLanguageCode);

        await _firstLaunchLanguagePopupOverlay.FadeTo(1, 180);
    }

    private async Task CloseFirstLaunchLanguagePopupAsync()
    {
        if (_firstLaunchLanguagePopupOverlay == null)
        {
            _isLanguageSelectionPromptVisible = false;
            return;
        }

        var overlay = _firstLaunchLanguagePopupOverlay;
        _firstLaunchLanguagePopupOverlay = null;

        await overlay.FadeTo(0, 140);

        if (Content is Grid mainGrid)
        {
            mainGrid.Children.Remove(overlay);
        }

        _firstLaunchLanguageOptionsContainer = null;
        _firstLaunchLanguageOptions.Clear();
        _firstLaunchLanguageChecks.Clear();
        _isLanguageSelectionPromptVisible = false;
    }

    private void BuildFirstLaunchLanguageOptions(IReadOnlyCollection<LanguageModel> languages)
    {
        if (_firstLaunchLanguageOptionsContainer == null)
        {
            return;
        }

        _firstLaunchLanguageOptionsContainer.Children.Clear();
        _firstLaunchLanguageOptions.Clear();
        _firstLaunchLanguageChecks.Clear();

        foreach (var language in languages)
        {
            var languageCode = CanonicalizeLanguageCode(language.LanguageCode);

            var optionBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#F1EDE9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(14, 16)
            };

            optionBorder.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await OnFirstLaunchLanguageOptionTappedAsync(languageCode))
            });

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };

            row.Add(new Label
            {
                Text = GetFlagByLanguageCode(languageCode),
                FontSize = 22,
                VerticalOptions = LayoutOptions.Center
            }, 0, 0);

            row.Add(new Label
            {
                Text = GetLanguageDisplayName(languageCode),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            var checkLabel = new Label
            {
                Text = "✓",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F48C06"),
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            row.Add(checkLabel, 2, 0);

            optionBorder.Content = row;

            _firstLaunchLanguageOptions[languageCode] = optionBorder;
            _firstLaunchLanguageChecks[languageCode] = checkLabel;
            _firstLaunchLanguageOptionsContainer.Children.Add(optionBorder);
        }
    }

    private void ApplyFirstLaunchLanguageSelectionStyle(string cultureCode)
    {
        foreach (var option in _firstLaunchLanguageOptions)
        {
            var isSelected = option.Key.Equals(cultureCode, StringComparison.OrdinalIgnoreCase);
            option.Value.BackgroundColor = isSelected
                ? Colors.White
                : Color.FromArgb("#F1EDE9");
            option.Value.StrokeThickness = isSelected ? 2 : 0;
            option.Value.Stroke = isSelected
                ? Color.FromArgb("#F48C06")
                : Colors.Transparent;

            if (_firstLaunchLanguageChecks.TryGetValue(option.Key, out var checkLabel))
            {
                checkLabel.IsVisible = isSelected;
            }
        }
    }

    private async Task OnFirstLaunchLanguageOptionTappedAsync(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return;
        }

        cultureCode = CanonicalizeLanguageCode(cultureCode);
        ApplyFirstLaunchLanguageSelectionStyle(cultureCode);

        await CloseFirstLaunchLanguagePopupAsync();
        await ApplySelectedLanguageAsync(cultureCode);
    }

    private async Task ApplySelectedLanguageAsync(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return;
        }

        cultureCode = CanonicalizeLanguageCode(cultureCode);
        var wasNarrating = _narrationFlowService.IsNarrating;

        Preferences.Set("language_selected", true);
        Preferences.Set("language", cultureCode);

        if (!_languageService.CurrentLanguage.Equals(cultureCode, StringComparison.OrdinalIgnoreCase))
        {
            _languageService.ChangeLanguage(cultureCode);
        }

        _lastAppliedLanguageCode = string.Empty;
        await RefreshPoiListForCurrentLanguageAsync();

        if (wasNarrating)
        {
            _narrationFlowService.StartNarration();
        }
    }

    private static string CanonicalizeLanguageCode(string? languageCode)
    {
        var normalized = (languageCode ?? string.Empty).Trim().Replace('_', '-').ToLowerInvariant();

        return normalized switch
        {
            "vi" or "vi-vn" => "vi-VN",
            "en" or "en-us" => "en-US",
            "zh" or "zh-cn" => "zh-CN",
            "ja" or "ja-jp" => "ja-JP",
            "ko" or "ko-kr" => "ko-KR",
            _ => string.IsNullOrWhiteSpace(languageCode) ? "vi-VN" : languageCode.Trim()
        };
    }

    private static List<LanguageModel> BuildDefaultLanguageOptions()
    {
        return new List<LanguageModel>
        {
            new() { LanguageCode = "vi-VN", LanguageName = "Tiếng Việt" },
            new() { LanguageCode = "en-US", LanguageName = "English" },
            new() { LanguageCode = "zh-CN", LanguageName = "中文" },
            new() { LanguageCode = "ja-JP", LanguageName = "日本語" },
            new() { LanguageCode = "ko-KR", LanguageName = "한국어" }
        };
    }

    private static List<LanguageModel> NormalizeLanguageOptions(
        IEnumerable<LanguageModel> source,
        string currentLanguage)
    {
        var merged = new Dictionary<string, LanguageModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in BuildDefaultLanguageOptions())
        {
            var code = CanonicalizeLanguageCode(item.LanguageCode);
            merged[code] = new LanguageModel
            {
                LanguageCode = code,
                LanguageName = item.LanguageName
            };
        }

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.LanguageCode))
            {
                continue;
            }

            var code = CanonicalizeLanguageCode(item.LanguageCode);
            if (!merged.TryGetValue(code, out var existing) || string.IsNullOrWhiteSpace(existing.LanguageName))
            {
                merged[code] = new LanguageModel
                {
                    LanguageCode = code,
                    LanguageName = item.LanguageName
                };
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.LanguageName))
            {
                existing.LanguageName = item.LanguageName;
            }
        }

        if (!merged.ContainsKey(currentLanguage))
        {
            merged[currentLanguage] = new LanguageModel
            {
                LanguageCode = currentLanguage,
                LanguageName = currentLanguage
            };
        }

        return merged.Values
            .OrderBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? GetLanguageResourceKey(string code)
    {
        return CanonicalizeLanguageCode(code).ToLowerInvariant() switch
        {
            "vi-vn" => "Vietnamese",
            "en-us" => "English",
            "ja-jp" => "Japanese",
            "ko-kr" => "Korean",
            "zh-cn" => "Chinese",
            _ => null
        };
    }

    private static string GetLanguageDisplayName(string code)
    {
        var resourceKey = GetLanguageResourceKey(code);
        if (resourceKey == null)
        {
            return CanonicalizeLanguageCode(code);
        }

        return LocalizationResourceManager.Instance[resourceKey];
    }

    private static string GetFlagByLanguageCode(string languageCode)
    {
        return CanonicalizeLanguageCode(languageCode).ToLowerInvariant() switch
        {
            "vi-vn" => "🇻🇳",
            "en-us" => "🇺🇸",
            "zh-cn" => "🇨🇳",
            "ko-kr" => "🇰🇷",
            "ja-jp" => "🇯🇵",
            _ => "🌐"
        };
    }

    // Hàm này sẽ đảm nhiệm việc khởi tạo các phần nặng của MainPage như load map, lấy dữ liệu POI, lấy vị trí hiện tại nếu chưa có, v.v. Hàm này được thiết kế để chạy nền sau khi UI đã kịp render frame đầu tiên để tránh giật lag lúc cold start, đồng thời có cơ chế tránh chạy lại nếu đã đang trong quá trình khởi tạo để đảm bảo hiệu quả và tránh xung đột trạng thái
    private async Task InitializeMainPageAsync()
    {
        if (_isInitializingMainPage)
        {
            return;
        }

        _isInitializingMainPage = true;
        var sw = Stopwatch.StartNew();
        try
        {
            // Nhường 1 nhịp để UI kịp render frame đầu trước khi chạy tác vụ nặng.
            await Task.Yield();

            // Load map nền và hiển thị vị trí người dùng nếu đã có, tránh việc phải chờ load map mỗi khi quay lại MainPage.
            if (!_isMapLoaded)
            {
                await MapHelper.LoadMapAsync(mapControl, _poiService, _locationService, initialZoomLevel: 19);
                _isMapLoaded = true;
                LogPerf("Initialize: map loaded", sw);
            }
            
            // Lấy dữ liệu POI nền nếu chưa có, tránh việc phải chờ load lại mỗi khi quay lại MainPage.
            if (!_isPoiListBound)
            {
                var poisData = await _poiService.GetAllPOIsAsync();
                _allPois = poisData;
                _isPoiListBound = true;
                _lastAppliedLanguageCode = _languageService.CurrentLanguage;
                _currentPoiPageIndex = 0;
                ApplyPoiFilterAndRefresh();
                LogPerf($"Initialize: POI list bound ({poisData.Count})", sw);
            }

            // Nếu chưa có vị trí nào được biết đến, cố gắng lấy vị trí hiện tại để cập nhật UI ngay khi khởi tạo xong, tránh việc phải chờ sự kiện LocationChanged mới để có UI chính xác khi người dùng vừa mở app lên đã ở gần POI.
            if (_lastKnownLocation == null)
            {
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                _lastKnownLocation = currentLocation;
                if (currentLocation != null)
                {
                    UpdateUIByLocation(currentLocation);
                }
                LogPerf("Initialize: first location acquired", sw);
            }

            LogPerf("Initialize: completed", sw);
        }
        finally
        {
            _isInitializingMainPage = false;
        }
    }

    // Chỉ delay 1 lần duy nhất trong session
    private async Task StartTrackingDeferredAsync()
    {
        try
        {
            if (!_hasAppliedStartupTrackingDelay)
            {
                _hasAppliedStartupTrackingDelay = true;
                await Task.Delay(AppSettings.StartupTrackingDelayMs);
            }

            await _locationService.StartTrackingAsync();
        }
        catch
        {
            // Ignore startup tracking failures to keep UI responsive.
        }
    }


    // Lấy vị trí hiện tại ngay lập tức (on-demand) để cập nhật UI sớm, thay vì phải chờ event tracking (LocationChanged)
    private async Task PrimeUiWithLatestLocationAsync()
    {
        try
        {
            var currentLocation = await _locationService.GetCurrentLocationAsync();
            if (currentLocation == null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _lastKnownLocation = currentLocation;
                UpdateUIByLocation(currentLocation);
            });

            await EnsurePoiDataReadyForUiAsync(currentLocation);
        }
        catch
        {
            // Ignore transient location read errors; tracking loop will update UI later.
        }
    }

    // đảm bảo dữ liệu POI đã sẵn sàng để UI có thể hiển thị và tương tác, nếu có vị trí thì cập nhật UI theo vị trí đó, tránh việc phải chờ load POI mỗi khi quay lại MainPage để có UI chính xác khi ở gần POI
    private async Task EnsurePoiDataReadyForUiAsync(Location location)
    {
        try
        {
            var pois = await _poiService.GetAllPOIsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allPois = pois;
                _isPoiListBound = true;
                _lastAppliedLanguageCode = _languageService.CurrentLanguage;
                ApplyPoiFilterAndRefresh(location);
                UpdateUIByLocation(location);
            });
        }
        catch
        {
            // Ignore background preload failures; existing UI state stays usable.
        }
    }

    private async Task RefreshPoiListForCurrentLanguageAsync()
    {
        try
        {
            var pois = await _poiService.GetAllPOIsAsync();
            var focusLocation = _lastKnownLocation ?? _locationService.LastKnownLocation;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allPois = pois;
                _isPoiListBound = true;
                _lastAppliedLanguageCode = _languageService.CurrentLanguage;
                _currentPoiPageIndex = 0;
                ApplyPoiFilterAndRefresh(_lastKnownLocation);
            });

            if (_isMapLoaded)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MapHelper.LoadMapAsync(
                        mapControl,
                        _poiService,
                        _locationService,
                        initialLocation: focusLocation,
                        initialZoomLevel: 19);
                });

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateMapHighlightByCurrentFilter(focusLocation);
                });
            }
        }
        catch
        {
            // Keep current language data when refresh fails.
        }
    }

    protected override void OnDisappearing()
    {
        _locationService.LocationChanged -= OnLocationChangedForMap;
        base.OnDisappearing();
    }

    // Hàm xử lý khi thay đổi vị trí để cập nhật giao diện và thuyết minh
    private void OnLocationChangedForMap(object? sender, Location location)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lastKnownLocation = location;
            UpdateUIByLocation(location);
        });
    }

    private static void LogPerf(string message, Stopwatch sw)
    {
        // Console.WriteLine($"[Perf][MainPage] {message} at {sw.ElapsedMilliseconds} ms");
    }

    // Hàm xử lý khi nhấn nút bắt đầu thuyết minh
    private async void OnNarratorTapped(object sender, EventArgs e)
    {
        var animateTapTask = AnimateNarratorButtonTapAsync();

        if (!_narrationFlowService.IsNarrating)
        {
            _narrationFlowService.StartNarration();
            // await _narrationFlowService.SmartPlayAsync();
        }
        else
        {
            // dừng thuyet minh
            _narrationFlowService.StopNarration();

        }
        // cập nhật lại UI của nút thuyết minh
        UpdateFloatingButtonUI();

        await animateTapTask;
    }
    
    // Hàm xử lý animation khi nhấn nút thuyết minh để có hiệu ứng phản hồi người dùng, giúp trải nghiệm mượt mà hơn
    private async Task AnimateNarratorButtonTapAsync()
    {
        await FloatingButton.ScaleToAsync(0.93, 80, Easing.CubicOut);
        await FloatingButton.ScaleToAsync(1, 80, Easing.CubicIn);
    }

    private void OnZoomInTapped(object sender, TappedEventArgs e)
    {
        AdjustMapZoom(0.7);
    }

    private void OnZoomOutTapped(object sender, TappedEventArgs e)
    {
        AdjustMapZoom(1.3);
    }

    private async void OnMyLocationTapped(object sender, TappedEventArgs e)
    {
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation == null)
        {
            return;
        }

        _lastKnownLocation = currentLocation;
        MapHelper.UpdateUserLocation(mapControl, currentLocation.Latitude, currentLocation.Longitude);
        CenterMapOn(currentLocation.Latitude, currentLocation.Longitude, 18);
    }

    // Hàm này để điều chỉnh zoom level của map khi người dùng nhấn nút zoom in/zoom out, có cơ chế giới hạn mức zoom tối đa và tối thiểu để tránh việc zoom quá gần hoặc quá xa, đồng thời giữ nguyên tâm map hiện tại khi zoom để tránh việc mất phương hướng
    private void AdjustMapZoom(double factor)
    {
        if (mapControl?.Map?.Navigator == null)
        {
            return;
        }

        var viewport = mapControl.Map.Navigator.Viewport;
        var minResolution = ToResolution(20);
        var maxResolution = ToResolution(3);
        var targetResolution = Math.Clamp(viewport.Resolution * factor, minResolution, maxResolution);
        var currentCenter = new MPoint(viewport.CenterX, viewport.CenterY);

        mapControl.Map.Navigator.CenterOnAndZoomTo(currentCenter, targetResolution);
        mapControl.Map.RefreshGraphics();
    }

    // Hàm này để căn giữa map vào tọa độ cụ thể với mức zoom nhất định, có cơ chế kiểm tra nếu map chưa sẵn sàng thì sẽ không thực hiện để tránh lỗi, đồng thời sử dụng phép chiếu Spherical Mercator để chuyển đổi từ kinh độ vĩ độ sang tọa độ map phù hợp
    private void CenterMapOn(double latitude, double longitude, int zoomLevel)
    {
        if (mapControl?.Map?.Navigator == null)
        {
            return;
        }

        var spherical = SphericalMercator.FromLonLat(longitude, latitude);
        var center = new MPoint(spherical.x, spherical.y);
        mapControl.Map.Navigator.CenterOnAndZoomTo(center, ToResolution(zoomLevel));
        mapControl.Map.RefreshGraphics();
    }

    // Hàm này để chuyển đổi từ zoom level sang resolution của map, có công thức dựa trên kích thước tile và hệ chiếu Spherical Mercator, đảm bảo rằng mỗi lần tăng 1 zoom level sẽ tương ứng với việc giảm một nửa resolution (tăng độ chi tiết) và ngược lại
    private static double ToResolution(int zoomLevel)
    {
        return 156543.03392 / Math.Pow(2, zoomLevel);
    }

    // Hàm xử lý khi nhấn vào icon user (chuyển đến Settings)
    private async void OnUserIconTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    // Hàm xử lý khi nhấn vào một POI trong danh sách để hiển thị chi tiết
    private async void OnPoiDetailTapped(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not POI selectedPoi)
        {
            return;
        }

        PoiList.SelectedItem = null;

        if (string.IsNullOrWhiteSpace(selectedPoi.restaurantId))
        {
            return;
        }

        var encodedId = Uri.EscapeDataString(selectedPoi.restaurantId);
        await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?restaurantId={encodedId}");
    }

    private void OnPreviousPageClicked(object sender, EventArgs e)
    {
        if (_currentPoiPageIndex <= 0)
        {
            return;
        }

        _currentPoiPageIndex--;
        BindPoiPage();
    }

    // dùng để xử lý khi người dùng nhấn nút trang tiếp theo trong phân trang danh sách POI, có cơ chế kiểm tra để tránh việc tăng chỉ số trang vượt quá tổng số trang hiện có dựa trên số lượng POI đã được lọc và kích thước trang, sau đó gọi hàm BindPoiPage để cập nhật lại danh sách hiển thị theo trang mới
    private void OnNextPageClicked(object sender, EventArgs e)
    {
        var totalPages = GetTotalPoiPages();
        if (_currentPoiPageIndex >= totalPages - 1)
        {
            return;
        }

        _currentPoiPageIndex++;
        BindPoiPage();
    }

    private void OnFilterAllTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.All);
    }

    private async void OnFilterNearTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Nearby);
        await RefreshNearbyFilterWithCurrentLocationAsync();
    }

    private void OnFilterFavoriteTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Favorite);
    }

    private void OnFilterOpenTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.OpenNow);
    }

    private void SetPoiFilter(PoiCategoryFilter filter)
    {
        _activePoiFilter = filter;
        _currentPoiPageIndex = 0;
        ApplyPoiFilterAndRefresh();
    }

    private async Task RefreshNearbyFilterWithCurrentLocationAsync()
    {
        var location = _lastKnownLocation ?? _locationService.LastKnownLocation;
        if (location == null)
        {
            location = await _locationService.GetCurrentLocationAsync();
        }

        if (location == null || _activePoiFilter != PoiCategoryFilter.Nearby)
        {
            return;
        }

        _lastKnownLocation = location;
        ApplyPoiFilterAndRefresh(location);
    }

    private void ApplyPoiFilterAndRefresh(Location? referenceLocation = null)
    {
        var location = referenceLocation ?? _lastKnownLocation ?? _locationService.LastKnownLocation;
        _filteredPois = GetFilteredPois(location);

        var totalPages = GetTotalPoiPages();
        if (_currentPoiPageIndex >= totalPages)
        {
            _currentPoiPageIndex = Math.Max(0, totalPages - 1);
        }

        BindPoiPage();
        UpdateCategoryChipUi();
        UpdateMapHighlightByCurrentFilter(location);
    }

    // Hàm này để lọc danh sách POI dựa trên filter đang được áp dụng và vị trí tham chiếu nếu có, có các nhánh xử lý tương ứng cho từng loại filter như Nearby sẽ lọc dựa trên khoảng cách đến vị trí tham chiếu, Favorite sẽ lọc dựa trên danh sách yêu thích, OpenNow sẽ lọc dựa trên trạng thái mở cửa hiện tại của POI, và All sẽ trả về tất cả POI mà không áp dụng filter nào
    private List<POI> GetFilteredPois(Location? location)
    {
        if (_allPois.Count == 0)
        {
            return new List<POI>();
        }

        IEnumerable<POI> query = _allPois;
        switch (_activePoiFilter)
        {
            case PoiCategoryFilter.Nearby:
                if (location == null)
                {
                    return new List<POI>();
                }

                query = query.Where(p => _poiService.GetDistanceMeters(location, p) <= NearbyFilterRadiusMeters);
                break;

            case PoiCategoryFilter.Favorite:
                var favoriteIds = _favoriteService
                    .GetFavorites()
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                query = query.Where(p => !string.IsNullOrWhiteSpace(p.restaurantId) && favoriteIds.Contains(p.restaurantId));
                break;

            case PoiCategoryFilter.OpenNow:
                query = query.Where(p => p.IsCurrentlyOpen);
                break;

            case PoiCategoryFilter.All:
            default:
                break;
        }

        return query.ToList();
    }

    // Lấy danh sách ID của các POI (restaurant) đang hiển thị theo filter hiện tại
    private IEnumerable<string>? GetVisiblePoiIdsForCurrentFilter()
    {
        if (_activePoiFilter == PoiCategoryFilter.All)
        {
            return null;
        }

        return _filteredPois
            .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
            .Select(p => p.restaurantId)
            .ToList();
    }

    // cập nhật highlight trên map dựa trên filter hiện tại và vị trí tham chiếu nếu có, sẽ xác định POI nào nên được highlight (ví dụ POI gần nhất trong trường hợp filter Nearby) và gọi helper để cập nhật highlight trên map, nếu không có POI nào phù hợp để highlight thì sẽ xóa highlight
    private void UpdateMapHighlightByCurrentFilter(Location? location)
    {
        if (!_isMapLoaded)
        {
            return;
        }

        var highlightCandidates = _activePoiFilter == PoiCategoryFilter.All
            ? _allPois
            : _filteredPois;

        var nearest = location == null
            ? null
            : _poiService.GetNearestPOI(location, highlightCandidates);

        var shouldHighlight = location != null
            && nearest != null
            && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;

        MapHelper.HighlightPOI(
            mapControl,
            shouldHighlight ? nearest : null,
            visiblePoiIds: GetVisiblePoiIdsForCurrentFilter());
    }

    // Cập nhật trạng thái ẩn/hiện của FloatingButton dựa trên khoảng cách đến POI gần nhất
    private void UpdateCategoryChipUi()
    {
        SetChipState(MainFilterAllChip, MainFilterAllLabel, _activePoiFilter == PoiCategoryFilter.All);
        SetChipState(MainFilterNearChip, MainFilterNearLabel, _activePoiFilter == PoiCategoryFilter.Nearby, MainFilterNearIcon);
        SetChipState(MainFilterFavoriteChip, MainFilterFavoriteLabel, _activePoiFilter == PoiCategoryFilter.Favorite, MainFilterFavoriteIcon);
        SetChipState(MainFilterOpenChip, MainFilterOpenLabel, _activePoiFilter == PoiCategoryFilter.OpenNow, MainFilterOpenIcon);
    }

    // Hàm này để cập nhật trạng thái ẩn/hiện và màu sắc của chip filter dựa trên việc chip đó có đang được chọn (active) hay không, nếu có icon đi kèm thì cũng sẽ cập nhật màu sắc của icon để đồng bộ với trạng thái của chip, giúp người dùng dễ dàng nhận biết filter nào đang được áp dụng
    private static void SetChipState(Border border, Label textLabel, bool isActive, Label? iconLabel = null)
    {
        border.BackgroundColor = isActive ? Color.FromArgb("#F48C06") : Color.FromArgb("#F5F1EE");
        textLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3E2723");
        if (iconLabel != null)
        {
            iconLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3E2723");
        }
    }

    // Hàm này để cập nhật danh sách POI hiển thị trên UI dựa trên trang hiện tại và danh sách POI đã được lọc, sẽ lấy ra phần tử tương ứng với trang hiện tại dựa trên kích thước trang và cập nhật ItemsSource của list, sau đó gọi hàm để cập nhật UI của phân trang (ví dụ trạng thái enabled/disabled của nút trang tiếp theo/trước đó, hiển thị số trang hiện tại, v.v.)
    private void BindPoiPage()
    {
        if (_filteredPois.Count == 0)
        {
            PoiList.ItemsSource = new List<POI>();
            UpdatePaginationUi();
            return;
        }

        var pageItems = _filteredPois
            .Skip(_currentPoiPageIndex * FeaturedPoiPageSize)
            .Take(FeaturedPoiPageSize)
            .ToList();

        PoiList.ItemsSource = pageItems;
        UpdatePaginationUi();
    }

    // Hàm này để tính toán tổng số trang hiện có dựa trên số lượng POI đã được lọc và kích thước trang, có cơ chế đảm bảo rằng nếu không có POI nào sau khi lọc thì vẫn sẽ trả về 1 trang để UI có thể hiển thị trạng thái "không có kết quả" thay vì bị lỗi hoặc không hiển thị gì
    private int GetTotalPoiPages()
    {
        if (_filteredPois.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)_filteredPois.Count / FeaturedPoiPageSize);
    }


    // cập nhật UI nút phân trang, dựa trên số lượng poi đã lọc và chỉ số trang hiện tại
    private void UpdatePaginationUi()
    {
        var totalPages = GetTotalPoiPages();
        var currentPageDisplay = totalPages == 0 ? 0 : _currentPoiPageIndex + 1;

        PaginationContainer.IsVisible = _filteredPois.Count > FeaturedPoiPageSize;
        PageIndicatorLabel.Text = $"Trang {currentPageDisplay}/{totalPages}";

        var canGoPrevious = _currentPoiPageIndex > 0;
        var canGoNext = _currentPoiPageIndex < totalPages - 1;

        PreviousPageButton.IsEnabled = canGoPrevious;
        PreviousPageButton.Opacity = canGoPrevious ? 1 : 0.5;

        NextPageButton.IsEnabled = canGoNext;
        NextPageButton.Opacity = canGoNext ? 1 : 0.5;
    }

    // Cập nhật trạng thái ẩn/hiện của FloatingButton dựa trên khoảng cách đến POI gần nhất
    private void UpdateUIByLocation(Location location)
    {
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);

        var shouldShow = nearest != null
            && _poiService.GetDistanceMeters(location, nearest) <= AppSettings.TriggerDistanceMeters;

        _lastFloatingButtonVisibility = shouldShow;

        if (_isInsidePOIUI != shouldShow)
        {
            _isInsidePOIUI = shouldShow;
            FloatingButton.IsVisible = shouldShow;
        }

        UpdateFloatingButtonUI();

        if (_isMapLoaded)
        {
            try
            {
                MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);
                MapHelper.CenterOnUserLocation(mapControl, location.Latitude, location.Longitude);
            }
            catch (Exception)
            {
                // Ignore transient map-camera errors while map is attaching/re-rendering.
            }
        }

        if (_isPoiListBound && _activePoiFilter == PoiCategoryFilter.Nearby)
        {
            ApplyPoiFilterAndRefresh(location);
            return;
        }

        UpdateMapHighlightByCurrentFilter(location);
    }

    // Cập nhật trạng thái của nút thuyết minh dựa trên trạng thái hiện tại của NarrationFlowService
    private void UpdateFloatingButtonUI()
    {
        NarratorButton.IsEnabled = true;
        FloatingButton.Opacity = 1;

        if (_narrationFlowService.IsNarrating)
        {
            NarratorText.Text = LocalizationResourceManager.Instance["StopNarration"];
        }
        else
        {
            NarratorText.Text = LocalizationResourceManager.Instance["StartNarration"];
        }
    }
}
