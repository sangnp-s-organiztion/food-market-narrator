namespace food_market_narrator.Models;

public class UiTranslationModel
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public int LanguageId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
}
