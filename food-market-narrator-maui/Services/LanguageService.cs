using System.Globalization;
using System.Net.Http.Json;
using food_market_narrator.Models;
using food_market_narrator.Resources;
using food_market_narrator.Resources.Localization;
using food_market_narrator.Settings;
using System.Text.Json;

namespace food_market_narrator.Services;

public class LanguageService : ILanguageService
{

    private const string LANGUAGE_KEY = "AppLanguage";
    private readonly HttpClient _httpClient;
    private List<LanguageModel>? _cachedLanguages;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public LanguageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string CurrentLanguage
    {
        get
        {
            Console.WriteLine($"CurrentLanguage getter called, returning: {Preferences.Get(LANGUAGE_KEY, "vi-VN")}");
            return Preferences.Get(LANGUAGE_KEY, "vi-VN"); 
            // mặc định tiếng Việt (vi-VN) nếu chưa có
        }
    }

    public async Task<List<LanguageModel>> GetAllLanguagesAsync()
    {
        if (_cachedLanguages != null && _cachedLanguages.Count > 0)
        {
            return _cachedLanguages;
        }

        var cachedLanguages = await ReadLanguagesCacheAsync();

        var baseCandidates = new List<string>();

        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

        foreach (var baseUrl in baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var requestUrl = new Uri(new Uri(baseUrl), AppSettings.LanguageEndpoint);
                Console.WriteLine($"[LanguageService] Trying URL = {requestUrl}");

                var data = await _httpClient.GetFromJsonAsync<List<LanguageModel>>(requestUrl);

                if (data == null || data.Count == 0)
                {
                    continue;
                }

                _cachedLanguages = data;
                await SaveLanguagesCacheAsync(_cachedLanguages);
                Console.WriteLine($"[LanguageService] Loaded {_cachedLanguages.Count} languages.");
                return _cachedLanguages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanguageService] Request failed: {baseUrl} -> {ex.Message}");
            }
        }

        if (cachedLanguages.Count > 0)
        {
            _cachedLanguages = cachedLanguages;
            Console.WriteLine($"[LanguageService] Loaded {_cachedLanguages.Count} languages from offline cache.");
            return _cachedLanguages;
        }

        return new List<LanguageModel>();
    }

    private static string GetLanguagesCacheFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "languages.json");
    }

    private static async Task<List<LanguageModel>> ReadLanguagesCacheAsync()
    {
        var path = GetLanguagesCacheFilePath();
        if (!File.Exists(path))
        {
            return new List<LanguageModel>();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<LanguageModel>>(stream, JsonOptions);
            return data ?? new List<LanguageModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LanguageService] Read cache failed: {ex.Message}");
            return new List<LanguageModel>();
        }
    }

    private static async Task SaveLanguagesCacheAsync(List<LanguageModel> languages)
    {
        try
        {
            var path = GetLanguagesCacheFilePath();
            var tempPath = $"{path}.tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, languages, JsonOptions);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LanguageService] Save cache failed: {ex.Message}");
        }
    }

    public async Task<LanguageModel?> GetLanguageByCodeAsync(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var allLanguages = await GetAllLanguagesAsync();
        return allLanguages.FirstOrDefault(x =>
            string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
    }

	// Thay đổi ngôn ngữ ứng dụng
	public void ChangeLanguage(string cultureCode)
    {
		// Lưu lại để lần sau app mở tự load
        Preferences.Set("AppLanguage", cultureCode);

        var culture = new CultureInfo(cultureCode);

        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        AppResources.Culture = culture;

        // Reload AppShell (nhẹ hơn recreate toàn app)
        if (Application.Current?.Windows?.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }

    }
}


