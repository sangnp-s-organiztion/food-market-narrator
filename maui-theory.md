# Lý thuyết giao diện .NET MAUI (chi tiết thực chiến)

Tài liệu này đi sâu đúng các phần bạn yêu cầu:
- Nắm vững `Grid`, `StackLayout`, `CollectionView`.
- Thuần thục `Binding` + `INotifyPropertyChanged`.
- Dùng `Command` thay event click.
- Tổ chức styles + theme Light/Dark.
- Tối ưu hiệu năng khi list dài và ảnh nhiều.
- Chi tiết `FlexLayout`.
- Liệt kê thuộc tính quan trọng của `Label`, `Button` và các control tương tự.

---

## 1) Tư duy nền tảng UI MAUI

MAUI UI thường gồm 3 lớp:
1. **View (XAML)**: hiển thị.
2. **ViewModel (C#)**: state + command.
3. **Model/Service**: dữ liệu/API.

Mục tiêu:
- UI chỉ bind dữ liệu + command.
- Không nhét logic nghiệp vụ vào code-behind.
- Dùng style/resource để tránh lặp.

---

## 2) `Grid` chi tiết

`Grid` là layout quan trọng nhất khi màn hình có nhiều vùng.

### 2.1. Thuộc tính chính
- `RowDefinitions`: định nghĩa hàng (`Auto`, `*`, `2*`, số cố định).
- `ColumnDefinitions`: định nghĩa cột.
- `RowSpacing`, `ColumnSpacing`: khoảng cách giữa ô.
- `Padding`: khoảng cách trong grid.

### 2.2. Attached properties trên phần tử con
- `Grid.Row`
- `Grid.Column`
- `Grid.RowSpan`
- `Grid.ColumnSpan`

### 2.3. Quy tắc dùng `Auto` vs `*`
- `Auto`: vừa đủ theo nội dung.
- `*`: phần còn lại.
- Dùng nhiều `*` để chia tỷ lệ: `1*`, `2*`, `3*`.

### 2.4. Mẫu chuẩn
```xml
<Grid RowDefinitions="Auto,*" ColumnDefinitions="*,Auto" RowSpacing="8" ColumnSpacing="8" Padding="12">
	<Label Grid.Row="0" Grid.Column="0" Text="Tiêu đề" FontAttributes="Bold" />
	<Button Grid.Row="0" Grid.Column="1" Text="Lọc" />
	<CollectionView Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" />
</Grid>
```

### 2.5. Lỗi thường gặp
- Lồng quá nhiều layout trong mỗi cell.
- Dùng quá nhiều `Auto` làm đo layout nặng.
- Quên `RowSpan/ColumnSpan` khiến UI vỡ bố cục.

---

## 3) `StackLayout` chi tiết (`VerticalStackLayout` / `HorizontalStackLayout`)

### 3.1. Dùng khi nào
- Form đơn giản, ít vùng.
- Danh sách control xếp 1 chiều.

### 3.2. Thuộc tính chính
- `Spacing`
- `Padding`
- `HorizontalOptions`, `VerticalOptions`

### 3.3. Ưu/nhược điểm
- Ưu: nhanh, dễ đọc.
- Nhược: nếu lồng sâu nhiều tầng sẽ khó tối ưu hơn `Grid`.

### 3.4. Quy tắc thực tế
- Màn hình có hơn 2 vùng phức tạp: ưu tiên `Grid`.
- Dùng `VerticalStackLayout` + `ScrollView` cho form dài.

---

## 4) `FlexLayout` chi tiết

`FlexLayout` phù hợp khi cần wrap và canh linh hoạt kiểu web flexbox.

### 4.1. Thuộc tính container
- `Direction`: `Row`, `Column`, `RowReverse`, `ColumnReverse`.
- `Wrap`: `NoWrap`, `Wrap`, `Reverse`.
- `JustifyContent`: canh trục chính (`Start`, `Center`, `SpaceBetween`, ...).
- `AlignItems`: canh theo trục phụ.
- `AlignContent`: canh nhiều dòng khi wrap.

### 4.2. Attached properties trên item
- `FlexLayout.Grow`: item giãn thêm khi còn chỗ.
- `FlexLayout.Shrink`: item co khi thiếu chỗ.
- `FlexLayout.Basis`: kích thước cơ sở.
- `FlexLayout.Order`: đổi thứ tự hiển thị.
- `FlexLayout.AlignSelf`: override riêng từng item.

### 4.3. Khi nào nên dùng
- Tag/chip list tự xuống dòng.
- Layout dashboard co giãn theo độ rộng.
- Nhiều card có chiều rộng linh hoạt.

### 4.4. Ví dụ
```xml
<FlexLayout Direction="Row" Wrap="Wrap" JustifyContent="Start" AlignItems="Center" BindableLayout.ItemsSource="{Binding Tags}">
	<BindableLayout.ItemTemplate>
		<DataTemplate>
			<Border Margin="4" Padding="8,4" StrokeShape="RoundRectangle 12">
				<Label Text="{Binding}" />
			</Border>
		</DataTemplate>
	</BindableLayout.ItemTemplate>
</FlexLayout>
```

---

## 5) `CollectionView` chi tiết (list chuẩn MAUI)

### 5.1. Thành phần chính
- `ItemsSource`: nguồn dữ liệu.
- `ItemTemplate`: template từng item.
- `EmptyView`: giao diện khi rỗng.
- `SelectionMode`: `None`, `Single`, `Multiple`.
- `SelectedItem` / `SelectedItems`.

### 5.2. Bố cục danh sách
- `ItemsLayout="VerticalList"` (mặc định).
- `ItemsLayout="HorizontalList"`.
- `GridItemsLayout` (dạng lưới).

### 5.3. Tương tác
- `SelectionChangedCommand`.
- `RemainingItemsThreshold` + `RemainingItemsThresholdReachedCommand` (load-more / infinite scroll).

### 5.4. Tối ưu template
- Item template càng nhẹ càng tốt.
- Tránh nested layout quá sâu.
- Ảnh nên có kích thước cố định (`WidthRequest/HeightRequest`).

### 5.5. Ví dụ load-more
```xml
<CollectionView
	ItemsSource="{Binding Pois}"
	RemainingItemsThreshold="5"
	RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
	<CollectionView.ItemTemplate>
		<DataTemplate>
			<Grid Padding="8" ColumnDefinitions="80,*" RowDefinitions="Auto,Auto">
				<Image Grid.RowSpan="2" WidthRequest="72" HeightRequest="72" Source="{Binding ThumbnailUrl}" Aspect="AspectFill" />
				<Label Grid.Column="1" Text="{Binding Name}" FontAttributes="Bold" />
				<Label Grid.Row="1" Grid.Column="1" Text="{Binding Description}" LineBreakMode="TailTruncation" MaxLines="2" />
			</Grid>
		</DataTemplate>
	</CollectionView.ItemTemplate>
</CollectionView>
```

---

## 6) Binding + `INotifyPropertyChanged` (cốt lõi MVVM)

### 6.1. Các mode binding thường dùng
- `OneWay`: ViewModel -> View.
- `TwoWay`: View <-> ViewModel (input).
- `OneTime`: bind 1 lần.

### 6.2. Thuộc tính bắt buộc khi bind
- ViewModel phải phát tín hiệu thay đổi qua `PropertyChanged`.
- Nếu không, UI không refresh.

### 6.3. Mẫu `INotifyPropertyChanged`
```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class BaseViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(storage, value))
			return false;

		storage = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

### 6.4. ViewModel ví dụ
```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;

public class MainViewModel : BaseViewModel
{
	private string searchText = string.Empty;
	private bool isBusy;

	public string SearchText
	{
		get => searchText;
		set => SetProperty(ref searchText, value);
	}

	public bool IsBusy
	{
		get => isBusy;
		set => SetProperty(ref isBusy, value);
	}

	public ObservableCollection<string> Items { get; } = new();

	public ICommand SearchCommand { get; }

	public MainViewModel()
	{
		SearchCommand = new Command(async () => await SearchAsync(), () => !IsBusy);
	}

	private async Task SearchAsync()
	{
		if (IsBusy) return;
		IsBusy = true;
		((Command)SearchCommand).ChangeCanExecute();

		try
		{
			await Task.Delay(300);
			Items.Clear();
			Items.Add($"Kết quả cho: {SearchText}");
		}
		finally
		{
			IsBusy = false;
			((Command)SearchCommand).ChangeCanExecute();
		}
	}
}
```

### 6.5. XAML bind mẫu
```xml
<VerticalStackLayout Padding="16" Spacing="10">
	<Entry Text="{Binding SearchText, Mode=TwoWay}" Placeholder="Nhập từ khóa" />
	<Button Text="Tìm" Command="{Binding SearchCommand}" />
	<ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />
	<CollectionView ItemsSource="{Binding Items}" />
</VerticalStackLayout>
```

---

## 7) Dùng `Command` thay event click

### 7.1. Vì sao nên dùng `Command`
- Tách UI khỏi logic.
- Dễ test ViewModel.
- Quản lý trạng thái enable/disable qua `CanExecute`.

### 7.2. Cách dùng
- Trong ViewModel: tạo `ICommand` (`Command`, `Command<T>`).
- Trong XAML: bind `Command` + `CommandParameter`.

### 7.3. Ví dụ truyền parameter
```xml
<Button Text="Xóa"
		Command="{Binding RemoveCommand}"
		CommandParameter="{Binding .}" />
```

```csharp
public ICommand RemoveCommand { get; }

public MainViewModel()
{
	RemoveCommand = new Command<string>(item =>
	{
		if (string.IsNullOrWhiteSpace(item)) return;
		Items.Remove(item);
	});
}
```

---

## 8) Tổ chức Styles + Theme (Light/Dark)

### 8.1. Cấu trúc khuyên dùng
- `Resources/Styles/Colors.xaml`: định nghĩa màu theo semantic.
- `Resources/Styles/Styles.xaml`: định nghĩa style control.
- `App.xaml`: merge resource dictionaries.

### 8.2. Dùng `AppThemeBinding`
```xml
<Color x:Key="PageBackgroundColor">#FFFFFF</Color>
<Color x:Key="PageBackgroundColorDark">#101418</Color>

<Style TargetType="ContentPage">
	<Setter Property="BackgroundColor"
			Value="{AppThemeBinding Light={StaticResource PageBackgroundColor}, Dark={StaticResource PageBackgroundColorDark}}" />
</Style>
```

### 8.3. Dùng `DynamicResource`
- Khi đổi theme runtime, `DynamicResource` cập nhật theo resource mới.
- `StaticResource` lấy tại thời điểm load (nhanh hơn chút, nhưng ít linh hoạt).

### 8.4. Semantic color (khuyên dùng)
Không đặt tên kiểu `Blue500`, mà đặt theo ý nghĩa:
- `ColorPrimary`
- `ColorOnPrimary`
- `ColorSurface`
- `ColorTextPrimary`
- `ColorError`

---

## 9) Tối ưu hiệu năng list dài + ảnh nhiều

### 9.1. Với `CollectionView`
- Ưu tiên `CollectionView` thay `ScrollView` chứa danh sách lớn.
- Không bọc `CollectionView` trong `ScrollView`.
- Item template đơn giản, hạn chế layout lồng nhiều cấp.
- Tránh trigger animation nặng trên mỗi item.

### 9.2. Với ảnh
- Dùng thumbnail thay ảnh full ở list.
- Cố định kích thước hiển thị để giảm re-layout.
- Lazy load khi scroll gần cuối (`RemainingItemsThreshold`).
- Cache ảnh (thư viện/giải pháp cache tùy dự án).

### 9.3. Với binding dữ liệu
- Dùng `ObservableCollection<T>` để cập nhật incremental.
- Không replace cả list liên tục khi chỉ đổi vài item.
- Tránh raise `PropertyChanged` dư thừa.

### 9.4. Với UI thread
- Tác vụ nặng (API, parse, xử lý ảnh) chạy background.
- Chỉ cập nhật UI trên main thread.

### 9.5. Checklist nhanh
- [ ] Item template đã tối giản.
- [ ] Ảnh có kích thước cố định.
- [ ] Có load-more/phân trang.
- [ ] Không lồng scroll sai cách.
- [ ] Không bind dư thừa.

---

## 10) Thuộc tính chung của đa số control

### 10.1. Kích thước & vị trí
- `WidthRequest`, `HeightRequest`
- `MinimumWidthRequest`, `MinimumHeightRequest`
- `MaximumWidthRequest`, `MaximumHeightRequest`
- `Margin`
- `Padding` (nếu control hỗ trợ)

### 10.2. Căn chỉnh
- `HorizontalOptions` (`Start`, `Center`, `End`, `Fill` + `AndExpand`)
- `VerticalOptions`

### 10.3. Hiển thị
- `BackgroundColor`
- `Opacity`
- `IsVisible`
- `ZIndex`
- `Clip`
- `Shadow`

### 10.4. Tương tác
- `IsEnabled`
- `InputTransparent`
- `GestureRecognizers` (`TapGestureRecognizer`, ...)

### 10.5. Automation & truy cập
- `AutomationId`
- `SemanticProperties.Description`
- `SemanticProperties.Hint`

---

## 11) `Label` thuộc tính chi tiết

### 11.1. Nội dung
- `Text`
- `FormattedText`

### 11.2. Font & màu
- `FontFamily`
- `FontSize`
- `FontAttributes`
- `CharacterSpacing`
- `TextColor`

### 11.3. Căn chỉnh
- `HorizontalTextAlignment`
- `VerticalTextAlignment`

### 11.4. Dòng & cắt chữ
- `LineBreakMode`
- `MaxLines`

### 11.5. Trang trí
- `TextDecorations`

### 11.6. Ví dụ
```xml
<Label Text="Mô tả sản phẩm"
	   FontSize="14"
	   TextColor="Gray"
	   LineBreakMode="TailTruncation"
	   MaxLines="2" />
```

---

## 12) `Button` thuộc tính chi tiết

### 12.1. Nội dung
- `Text`
- `ImageSource`
- `ContentLayout` (vị trí ảnh + text)

### 12.2. Tương tác
- `Command`
- `CommandParameter`
- `Pressed`
- `Released`
- `Clicked` (nên ưu tiên `Command` trong MVVM)

### 12.3. Trạng thái giao diện
- `IsEnabled`
- `BackgroundColor`
- `TextColor`
- `BorderColor`
- `BorderWidth`
- `CornerRadius`
- `Padding`

### 12.4. Font
- `FontFamily`
- `FontSize`
- `FontAttributes`

### 12.5. Ví dụ
```xml
<Button Text="Lưu"
		Command="{Binding SaveCommand}"
		IsEnabled="{Binding CanSave}"
		CornerRadius="10"
		Padding="16,10" />
```

---

## 13) Các control tương tự thường dùng + thuộc tính chính

## 13.1. `Entry` (input 1 dòng)
- `Text`
- `Placeholder`, `PlaceholderColor`
- `Keyboard`
- `IsPassword`
- `MaxLength`
- `ClearButtonVisibility`
- `ReturnType`
- `Completed` (event)

## 13.2. `Editor` (input nhiều dòng)
- `Text`
- `Placeholder`, `PlaceholderColor`
- `AutoSize`
- `MaxLength`
- `Keyboard`

## 13.3. `SearchBar`
- `Text`
- `Placeholder`
- `SearchCommand`
- `SearchCommandParameter`
- `CancelButtonColor`

## 13.4. `Image`
- `Source`
- `Aspect` (`AspectFit`, `AspectFill`, `Fill`, `Center`)
- `IsAnimationPlaying` (gif)

## 13.5. `ImageButton`
- `Source`
- `Command`, `CommandParameter`
- `Pressed`, `Released`, `Clicked`
- `Aspect`
- `CornerRadius`

## 13.6. `CollectionView`
- `ItemsSource`
- `ItemTemplate`
- `ItemsLayout`
- `SelectionMode`, `SelectedItem`, `SelectedItems`
- `SelectionChangedCommand`
- `EmptyView`
- `RemainingItemsThreshold`, `RemainingItemsThresholdReachedCommand`

## 13.7. `Picker`
- `ItemsSource`
- `ItemDisplayBinding`
- `SelectedIndex`
- `SelectedItem`
- `Title`

## 13.8. `DatePicker` / `TimePicker`
- `Date` / `Time`
- `MinimumDate`, `MaximumDate` (với `DatePicker`)
- `Format`

## 13.9. `Switch`
- `IsToggled`
- `OnColor`
- `ThumbColor`
- `Toggled` (event)

## 13.10. `CheckBox`
- `IsChecked`
- `Color`
- `CheckedChanged` (event)

## 13.11. `Slider`
- `Minimum`, `Maximum`
- `Value`
- `ThumbColor`
- `MinimumTrackColor`, `MaximumTrackColor`

## 13.12. `Stepper`
- `Minimum`, `Maximum`
- `Increment`
- `Value`

## 13.13. `ProgressBar`
- `Progress` (0.0 -> 1.0)
- `ProgressColor`

## 13.14. `ActivityIndicator`
- `IsRunning`
- `Color`

## 13.15. `ScrollView`
- `Orientation`
- `ScrollX`, `ScrollY`
- `Scrolled` (event)

## 13.16. `RefreshView`
- `IsRefreshing`
- `Command`
- `CommandParameter`
- `RefreshColor`

## 13.17. `WebView`
- `Source`
- `CanGoBack`, `CanGoForward`
- `Reload()`

## 13.18. `Map` (nếu dùng MAUI Maps)
- `MapType`
- `IsShowingUser`
- `Pins`
- `MoveToRegion(...)`

---

## 14) Mẫu trang tổng hợp (Grid + Binding + Command + CollectionView)

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
			 xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
			 x:Class="FoodMarketNarrator.Views.MapPage"
			 Title="Địa điểm">

	<Grid RowDefinitions="Auto,*" Padding="12" RowSpacing="10">
		<Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
			<Entry Grid.Column="0"
				   Text="{Binding SearchText, Mode=TwoWay}"
				   Placeholder="Tìm món / chợ / quán" />
			<Button Grid.Column="1"
					Text="Tìm"
					Command="{Binding SearchCommand}" />
		</Grid>

		<CollectionView Grid.Row="1"
						ItemsSource="{Binding Pois}"
						SelectionMode="Single"
						SelectionChangedCommand="{Binding OpenPoiCommand}">
			<CollectionView.ItemTemplate>
				<DataTemplate>
					<Grid Padding="8" ColumnDefinitions="88,*" RowDefinitions="Auto,Auto" ColumnSpacing="10">
						<Image Grid.RowSpan="2"
							   WidthRequest="80"
							   HeightRequest="80"
							   Source="{Binding ImageUrl}"
							   Aspect="AspectFill" />

						<Label Grid.Column="1"
							   Text="{Binding Name}"
							   FontAttributes="Bold"
							   LineBreakMode="TailTruncation"
							   MaxLines="1" />

						<Label Grid.Row="1"
							   Grid.Column="1"
							   Text="{Binding ShortDescription}"
							   TextColor="Gray"
							   LineBreakMode="TailTruncation"
							   MaxLines="2" />
					</Grid>
				</DataTemplate>
			</CollectionView.ItemTemplate>
		</CollectionView>
	</Grid>

</ContentPage>
```

---

## 15) Lộ trình học nhanh 7 ngày (gợi ý)

1. Ngày 1: `Grid`, `StackLayout`, `FlexLayout`.
2. Ngày 2: `Label`, `Button`, `Entry`, `Editor`.
3. Ngày 3: Binding modes + `INotifyPropertyChanged`.
4. Ngày 4: `Command`, `CommandParameter`, `CanExecute`.
5. Ngày 5: `CollectionView` + phân trang.
6. Ngày 6: Styles + theme Light/Dark.
7. Ngày 7: Tối ưu hiệu năng + refactor màn hình thật của dự án.

Nếu muốn, phần tiếp theo mình có thể viết thêm bộ **"20 lỗi MAUI UI hay gặp + cách sửa"** theo đúng project của bạn (map, location, narration).

