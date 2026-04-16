using System.ComponentModel;
using System.Globalization;
using food_market_narrator.Resources.Localization;

namespace food_market_narrator;

public sealed class LocalizationResourceManager : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationResourceManager> LazyInstance =
        new(() => new LocalizationResourceManager());

    public static LocalizationResourceManager Instance => LazyInstance.Value;

    private LocalizationResourceManager()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string textKey]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(textKey))
            {
                return string.Empty;
            }

            return AppResources.ResourceManager.GetString(textKey, AppResources.Culture)
                ?? textKey;
        }
    }

    public void SetCulture(CultureInfo culture)
    {
        AppResources.Culture = culture;

        // Notify all bindings that use indexer syntax: [SomeKey]
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
