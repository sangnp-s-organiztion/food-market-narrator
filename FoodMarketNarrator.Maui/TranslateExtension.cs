using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace food_market_narrator;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return new Binding(".");
        }

        return new Binding($"[{Key}]", source: LocalizationResourceManager.Instance, mode: BindingMode.OneWay);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
