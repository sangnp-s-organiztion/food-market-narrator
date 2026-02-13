
using food_market_narrator.Services;

public class AudioService
{
    private POIService _poiService = new POIService();
    private LocationServices _locationService = new LocationServices();
    private LanguageService _languageService = new LanguageService();





    // ================ Audio Methods ================

    public void PlaySound(string soundFile)
    {
        // Implementation to play sound
    }

    public void StopSound()
    {
        // Implementation to stop sound
    }

    // put audio in to queue
    public void QueueSound(string soundFile)
    {
        // Implementation to queue sound
    }
}