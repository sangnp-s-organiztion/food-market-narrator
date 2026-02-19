# MAUI UI Cheat Sheet (1 trang tra cứu nhanh)

Mục tiêu: nhìn nhanh thuộc tính cốt lõi + mẫu dùng ngắn cho UI MAUI.

---

## 1) Layout

### `Grid`
**Dùng khi:** màn hình có nhiều vùng, cần chia hàng/cột rõ ràng.

**Thuộc tính chính**
- `RowDefinitions`, `ColumnDefinitions`
- `RowSpacing`, `ColumnSpacing`, `Padding`
- Attached: `Grid.Row`, `Grid.Column`, `Grid.RowSpan`, `Grid.ColumnSpan`

**Ví dụ 1 dòng**
```xml
<Grid RowDefinitions="Auto,*" ColumnDefinitions="*,Auto"><Label Grid.Column="0" /><Button Grid.Column="1" /></Grid>
```

### `VerticalStackLayout` / `HorizontalStackLayout`
**Dùng khi:** form đơn giản, xếp 1 chiều.

**Thuộc tính chính**
- `Spacing`, `Padding`
- `HorizontalOptions`, `VerticalOptions`

**Ví dụ 1 dòng**
```xml
<VerticalStackLayout Spacing="8" Padding="12"><Entry /><Button Text="Lưu" /></VerticalStackLayout>
```

### `FlexLayout`
**Dùng khi:** card/tag/chip cần tự wrap như flexbox.

**Container props**
- `Direction`, `Wrap`
- `JustifyContent`, `AlignItems`, `AlignContent`

**Item attached props**
- `FlexLayout.Grow`, `FlexLayout.Shrink`, `FlexLayout.Basis`
- `FlexLayout.Order`, `FlexLayout.AlignSelf`

**Ví dụ 1 dòng**
```xml
<FlexLayout Direction="Row" Wrap="Wrap" JustifyContent="Start"><Label Text="Tag 1" /><Label Text="Tag 2" /></FlexLayout>
```

---

## 2) Control cốt lõi và thuộc tính

### `Label`
- Nội dung: `Text`, `FormattedText`
- Chữ: `FontSize`, `FontFamily`, `FontAttributes`, `CharacterSpacing`, `TextColor`
- Căn chỉnh: `HorizontalTextAlignment`, `VerticalTextAlignment`
- Dòng: `LineBreakMode`, `MaxLines`
- Trang trí: `TextDecorations`

**Ví dụ**
```xml
<Label Text="Mô tả" LineBreakMode="TailTruncation" MaxLines="2" FontAttributes="Bold" />
```

### `Button`
- Nội dung: `Text`, `ImageSource`, `ContentLayout`
- MVVM: `Command`, `CommandParameter`
- Event: `Clicked`, `Pressed`, `Released`
- Giao diện: `BackgroundColor`, `TextColor`, `CornerRadius`, `Padding`, `BorderColor`, `BorderWidth`

**Ví dụ**
```xml
<Button Text="Tìm" Command="{Binding SearchCommand}" IsEnabled="{Binding CanSearch}" />
```

### `Entry`
- `Text`, `Placeholder`, `PlaceholderColor`
- `Keyboard`, `IsPassword`, `MaxLength`
- `ClearButtonVisibility`, `ReturnType`, `Completed`

```xml
<Entry Text="{Binding Keyword, Mode=TwoWay}" Placeholder="Nhập từ khóa" ReturnType="Search" />
```

### `Editor`
- `Text`, `Placeholder`, `PlaceholderColor`
- `AutoSize`, `MaxLength`, `Keyboard`

```xml
<Editor Text="{Binding Note}" AutoSize="TextChanges" />
```

### `Image`
- `Source`, `Aspect`
- Kích thước: `WidthRequest`, `HeightRequest`

```xml
<Image Source="{Binding ThumbnailUrl}" WidthRequest="80" HeightRequest="80" Aspect="AspectFill" />
```

### `ImageButton`
- `Source`, `Command`, `CommandParameter`
- `Aspect`, `CornerRadius`, `Clicked`

```xml
<ImageButton Source="ic_play.png" Command="{Binding PlayCommand}" />
```

### `CollectionView`
- Dữ liệu: `ItemsSource`, `ItemTemplate`, `EmptyView`
- Chọn: `SelectionMode`, `SelectedItem`, `SelectionChangedCommand`
- Layout: `ItemsLayout` (`VerticalList`, `HorizontalList`, `GridItemsLayout`)
- Load-more: `RemainingItemsThreshold`, `RemainingItemsThresholdReachedCommand`

```xml
<CollectionView ItemsSource="{Binding Items}" RemainingItemsThreshold="5" RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}" />
```

### `Picker`
- `ItemsSource`, `ItemDisplayBinding`
- `SelectedIndex`, `SelectedItem`, `Title`

```xml
<Picker ItemsSource="{Binding Categories}" SelectedItem="{Binding SelectedCategory}" Title="Chọn danh mục" />
```

### `DatePicker` / `TimePicker`
- `DatePicker`: `Date`, `MinimumDate`, `MaximumDate`, `Format`
- `TimePicker`: `Time`, `Format`

```xml
<DatePicker Date="{Binding BookingDate}" MinimumDate="{x:Static sys:DateTime.Today}" />
```

### `Switch`
- `IsToggled`, `OnColor`, `ThumbColor`, `Toggled`

```xml
<Switch IsToggled="{Binding IsVoiceEnabled}" />
```

### `CheckBox`
- `IsChecked`, `Color`, `CheckedChanged`

```xml
<CheckBox IsChecked="{Binding AcceptTerms}" />
```

### `SearchBar`
- `Text`, `Placeholder`
- `SearchCommand`, `SearchCommandParameter`

```xml
<SearchBar Text="{Binding Query}" SearchCommand="{Binding SearchCommand}" />
```

### `Slider`
- `Minimum`, `Maximum`, `Value`
- `MinimumTrackColor`, `MaximumTrackColor`, `ThumbColor`

```xml
<Slider Minimum="0" Maximum="100" Value="{Binding Volume}" />
```

### `Stepper`
- `Minimum`, `Maximum`, `Increment`, `Value`

```xml
<Stepper Minimum="1" Maximum="10" Increment="1" Value="{Binding Count}" />
```

### `ProgressBar`
- `Progress` (0..1), `ProgressColor`

```xml
<ProgressBar Progress="{Binding UploadProgress}" />
```

### `ActivityIndicator`
- `IsRunning`, `Color`, `IsVisible`

```xml
<ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />
```

### `ScrollView`
- `Orientation`, `Scrolled`

```xml
<ScrollView Orientation="Vertical"><VerticalStackLayout /></ScrollView>
```

### `RefreshView`
- `IsRefreshing`, `Command`, `CommandParameter`, `RefreshColor`

```xml
<RefreshView IsRefreshing="{Binding IsRefreshing}" Command="{Binding RefreshCommand}"><CollectionView ItemsSource="{Binding Items}" /></RefreshView>
```

### `WebView`
- `Source`, `CanGoBack`, `CanGoForward`

```xml
<WebView Source="https://example.com" />
```

---

## 3) Binding + `INotifyPropertyChanged` (rất ngắn)

### Mẫu base VM
```csharp
public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
```

### Binding mode nhớ nhanh
- `OneWay`: VM -> UI
- `TwoWay`: UI <-> VM (input)
- `OneTime`: bind 1 lần

---

## 4) Command thay event click (mẫu nhanh)

```csharp
public ICommand SearchCommand { get; }

public MainViewModel()
{
    SearchCommand = new Command(async () => await SearchAsync(), () => !IsBusy);
}
```

```xml
<Button Text="Tìm" Command="{Binding SearchCommand}" />
```

Dùng `CanExecute` + `ChangeCanExecute()` để bật/tắt nút theo trạng thái.

---

## 5) Styles + Theme Light/Dark (mẫu nhanh)

### `AppThemeBinding`
```xml
<Style TargetType="Label">
    <Setter Property="TextColor"
            Value="{AppThemeBinding Light=Black, Dark=White}" />
</Style>
```

### `DynamicResource`
```xml
<Label Text="Xin chào" TextColor="{DynamicResource ColorTextPrimary}" />
```

Khuyên dùng tên semantic color:
- `ColorPrimary`, `ColorSurface`, `ColorTextPrimary`, `ColorError`

---

## 6) Hiệu năng list dài + ảnh nhiều (checklist 30s)

- Dùng `CollectionView`, không bọc trong `ScrollView`.
- Item template đơn giản, tránh layout lồng sâu.
- Ảnh dùng thumbnail + fixed size (`WidthRequest`, `HeightRequest`).
- Dùng load-more (`RemainingItemsThreshold`).
- Chỉ cập nhật item thay đổi, tránh replace cả list liên tục.
- Task nặng chạy background, UI update trên main thread.

---

## 7) Sơ đồ chọn layout nhanh

- Form đơn giản 1 chiều -> `VerticalStackLayout`
- Màn hình nhiều vùng/hàng-cột -> `Grid`
- Card/tag tự xuống dòng -> `FlexLayout`
- Danh sách lớn -> `CollectionView`

---

## 8) Bộ nhớ nhanh (5 câu)

1. UI phức tạp -> nghĩ `Grid` trước.
2. Danh sách dài -> luôn `CollectionView`.
3. Input -> `TwoWay` binding.
4. Nút bấm -> `Command` thay `Clicked` khi theo MVVM.
5. Theme -> style + semantic color + `AppThemeBinding`.
