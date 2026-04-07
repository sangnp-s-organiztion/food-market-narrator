using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;
using Microsoft.Extensions.Options;

namespace food_market_narrator_api.Services;

public class TranslationService
{
    private static readonly HashSet<string> UiSupportedLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "vi", "ja", "zh", "ko"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TranslationHistoryRepository _translationHistoryRepository;
    private readonly RestaurantRepository _restaurantRepository;
    private readonly LanguageRepository _languageRepository;
    private readonly AudioService _audioService;
    private readonly IWebHostEnvironment _environment;
    private readonly LibreTranslateSettings _libreTranslateSettings;
    private readonly EdgeTtsSettings _edgeTtsSettings;
    private readonly TranslationPricingSettings _translationPricingSettings;

    public TranslationService(
        IHttpClientFactory httpClientFactory,
        TranslationHistoryRepository translationHistoryRepository,
        RestaurantRepository restaurantRepository,
        LanguageRepository languageRepository,
        AudioService audioService,
        IWebHostEnvironment environment,
        IOptions<LibreTranslateSettings> libreTranslateOptions,
        IOptions<EdgeTtsSettings> edgeTtsOptions,
        IOptions<TranslationPricingSettings> pricingOptions)
    {
        _httpClientFactory = httpClientFactory;
        _translationHistoryRepository = translationHistoryRepository;
        _restaurantRepository = restaurantRepository;
        _languageRepository = languageRepository;
        _audioService = audioService;
        _environment = environment;
        _libreTranslateSettings = libreTranslateOptions.Value;
        _edgeTtsSettings = edgeTtsOptions.Value;
        _translationPricingSettings = pricingOptions.Value;
    }

    public async Task<TranslateTextResponse> TranslateAsync(int sellerUserId, string restaurantId, TranslateTextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text is required.");
        }

        await EnsureRestaurantOwnershipAsync(sellerUserId, restaurantId);

        var sourceCode = NormalizeSourceLanguageCode(request.SourceLanguageCode);
        var targetCode = NormalizeRequiredLanguageCode(request.TargetLanguageCode);
        var requestId = NormalizeRequestId(request.RequestId);
        var sourceText = request.Text.Trim();
        var sourceHash = ComputeSha256(sourceText);
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var translatedText = await TranslateWithLibreAsync(sourceText, sourceCode, targetCode);
            stopwatch.Stop();

            var finishedAtUtc = DateTime.UtcNow;
            var inputChars = sourceText.Length;
            var outputChars = translatedText.Length;
            var costAmount = CalculateEstimatedCost(inputChars);
            var billingMonth = finishedAtUtc.ToString("yyyy-MM");

            await _translationHistoryRepository.InsertTranslationJobAsync(new TranslationJobRecord
            {
                RequestId = requestId,
                SellerUserId = sellerUserId,
                RestaurantId = restaurantId,
                AudioId = null,
                SourceLanguageCode = sourceCode,
                TargetLanguageCode = targetCode,
                SourceTextHash = sourceHash,
                SourceCharCount = inputChars,
                Provider = "libretranslate",
                ProviderEndpoint = BuildEndpoint(_libreTranslateSettings.BaseUrl, _libreTranslateSettings.TranslatePath),
                Status = "success",
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAtUtc = finishedAtUtc
            });

            var usageEventId = Guid.NewGuid().ToString("N");
            await _translationHistoryRepository.InsertUsageLedgerAsync(new TranslationUsageLedgerRecord
            {
                UsageEventId = usageEventId,
                RequestId = requestId,
                SellerUserId = sellerUserId,
                RestaurantId = restaurantId,
                AudioId = null,
                Provider = "libretranslate",
                ActionType = "translate",
                UnitType = "chars",
                InputChars = inputChars,
                OutputChars = outputChars,
                BillableUnits = inputChars,
                RateVersion = _translationPricingSettings.RateVersion,
                PricePer1KUnits = _translationPricingSettings.PricePer1KChars,
                Currency = _translationPricingSettings.Currency,
                CostAmount = costAmount,
                TaxAmount = 0,
                TotalAmount = costAmount,
                Status = "billable",
                BillingMonth = billingMonth,
                CreatedAtUtc = finishedAtUtc
            });

            await _translationHistoryRepository.UpsertMonthlyBillingAsync(new MonthlyBillingSnapshotRecord
            {
                SellerUserId = sellerUserId,
                BillingMonth = billingMonth,
                TotalRequests = 1,
                SuccessRequests = 1,
                FailedRequests = 0,
                TotalBillableUnits = inputChars,
                TotalAmount = costAmount,
                Currency = _translationPricingSettings.Currency,
                LastRecomputedAtUtc = finishedAtUtc
            });

            return new TranslateTextResponse
            {
                RequestId = requestId,
                SourceLanguageCode = sourceCode,
                TargetLanguageCode = targetCode,
                TranslatedText = translatedText,
                InputChars = inputChars,
                OutputChars = outputChars,
                EstimatedCost = costAmount,
                Currency = _translationPricingSettings.Currency
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var failedAtUtc = DateTime.UtcNow;
            var billingMonth = failedAtUtc.ToString("yyyy-MM");

            await _translationHistoryRepository.InsertTranslationJobAsync(new TranslationJobRecord
            {
                RequestId = requestId,
                SellerUserId = sellerUserId,
                RestaurantId = restaurantId,
                AudioId = null,
                SourceLanguageCode = sourceCode,
                TargetLanguageCode = targetCode,
                SourceTextHash = sourceHash,
                SourceCharCount = sourceText.Length,
                Provider = "libretranslate",
                ProviderEndpoint = BuildEndpoint(_libreTranslateSettings.BaseUrl, _libreTranslateSettings.TranslatePath),
                Status = "failed",
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = failedAtUtc,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorCode = "translate_failed",
                ErrorMessage = ex.Message,
                CreatedAtUtc = failedAtUtc
            });

            var failedUsageEventId = Guid.NewGuid().ToString("N");
            await _translationHistoryRepository.InsertUsageLedgerAsync(new TranslationUsageLedgerRecord
            {
                UsageEventId = failedUsageEventId,
                RequestId = requestId,
                SellerUserId = sellerUserId,
                RestaurantId = restaurantId,
                AudioId = null,
                Provider = "libretranslate",
                ActionType = "translate",
                UnitType = "chars",
                InputChars = sourceText.Length,
                OutputChars = 0,
                BillableUnits = 0,
                RateVersion = _translationPricingSettings.RateVersion,
                PricePer1KUnits = _translationPricingSettings.PricePer1KChars,
                Currency = _translationPricingSettings.Currency,
                CostAmount = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                Status = "failed",
                BillingMonth = billingMonth,
                CreatedAtUtc = failedAtUtc
            });

            await _translationHistoryRepository.UpsertMonthlyBillingAsync(new MonthlyBillingSnapshotRecord
            {
                SellerUserId = sellerUserId,
                BillingMonth = billingMonth,
                TotalRequests = 1,
                SuccessRequests = 0,
                FailedRequests = 1,
                TotalBillableUnits = 0,
                TotalAmount = 0,
                Currency = _translationPricingSettings.Currency,
                LastRecomputedAtUtc = failedAtUtc
            });

            throw;
        }
    }

    public async Task<CreateAudioFromTextResponse> CreateAudioFromTextAsync(int sellerUserId, string restaurantId, CreateAudioFromTextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text is required.");
        }

        await EnsureRestaurantOwnershipAsync(sellerUserId, restaurantId);

        var languageCode = NormalizeRequiredLanguageCode(request.LanguageCode);
        var requestId = NormalizeRequestId(request.RequestId);
        var ttsText = request.Text.Trim();

        var languageId = await ResolveLanguageIdAsync(languageCode);
        if (languageId <= 0)
        {
            throw new ArgumentException($"Language '{request.LanguageCode}' is not configured in MSSQL Languages table.");
        }

        var ttsResult = await GenerateAudioWithEdgeTtsAsync(ttsText, languageCode, request.Voice);

        string webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadDir = Path.Combine(webRoot, "uploads", "audios");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"tts_{languageCode}_{Guid.NewGuid():N}.mp3";
        var fullPath = Path.Combine(uploadDir, fileName);
        await File.WriteAllBytesAsync(fullPath, ttsResult.AudioBytes);

        var audioUrl = $"/uploads/audios/{fileName}";
        var createdAudio = await _audioService.CreateAsync(restaurantId, languageId, audioUrl);

        var nowUtc = DateTime.UtcNow;
        var sourceText = string.IsNullOrWhiteSpace(request.SourceText) ? ttsText : request.SourceText.Trim();
        await _translationHistoryRepository.InsertTranslationVersionAsync(new AudioTranslationVersionRecord
        {
            SellerUserId = sellerUserId,
            RestaurantId = restaurantId,
            AudioId = createdAudio.AudioId,
            SourceLanguageCode = languageCode,
            TargetLanguageCode = languageCode,
            SourceText = sourceText,
            TranslatedText = ttsText,
            TranslatedTextHash = ComputeSha256(ttsText),
            VersionNo = createdAudio.Version,
            IsActive = createdAudio.IsActive,
            GenerationMethod = "edge-tts",
            JobId = requestId,
            UsageEventId = null,
            CreatedAtUtc = nowUtc,
            ActivatedAtUtc = createdAudio.IsActive ? nowUtc : null
        });

        return new CreateAudioFromTextResponse
        {
            RequestId = requestId,
            AudioId = createdAudio.AudioId,
            AudioUrl = createdAudio.AudioUrl,
            LanguageCode = languageCode,
            Voice = ttsResult.Voice,
            CreatedAt = nowUtc
        };
    }

    private async Task EnsureRestaurantOwnershipAsync(int sellerUserId, string restaurantId)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
        if (restaurant == null)
        {
            throw new KeyNotFoundException("Restaurant not found.");
        }

        if (restaurant.UserId != sellerUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this restaurant.");
        }
    }

    private async Task<int> ResolveLanguageIdAsync(string languageCode)
    {
        var allLanguages = await _languageRepository.GetAllLanguagesAsync();
        var exact = allLanguages.FirstOrDefault(x =>
            string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact.LanguageId;
        }

        var byBaseCode = allLanguages.FirstOrDefault(x =>
            string.Equals(NormalizeBaseLanguageCode(x.LanguageCode), languageCode, StringComparison.OrdinalIgnoreCase));

        return byBaseCode?.LanguageId ?? 0;
    }

    private async Task<string> TranslateWithLibreAsync(string text, string sourceCode, string targetCode)
    {
        var client = _httpClientFactory.CreateClient(nameof(TranslationService));
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_libreTranslateSettings.TimeoutSeconds, 5, 120));

        var languages = await GetLibreLanguagesAsync(client);
        var availableCodes = languages
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetProviderCode = ResolveProviderLanguageCode(targetCode, availableCodes, isTarget: true);

        string sourceProviderCode;
        if (string.Equals(sourceCode, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var guessedSourceUiCode = GuessLikelySourceLanguageCode(text);
            if (!string.IsNullOrWhiteSpace(guessedSourceUiCode)
                && TryResolveProviderLanguageCode(guessedSourceUiCode, availableCodes, out var guessedProviderCode))
            {
                sourceProviderCode = guessedProviderCode;
            }
            else
            {
                sourceProviderCode = "auto";
            }
        }
        else
        {
            sourceProviderCode = ResolveProviderLanguageCode(sourceCode, availableCodes, isTarget: false);
        }

        if (!string.Equals(sourceProviderCode, "auto", StringComparison.OrdinalIgnoreCase)
            && string.Equals(sourceProviderCode, targetProviderCode, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        if (!CanTranslateDirect(sourceProviderCode, targetProviderCode, languages))
        {
            throw new ArgumentException(
                $"LibreTranslate does not support direct translation from '{sourceProviderCode}' to '{targetProviderCode}'.");
        }

        var translated = await TranslateDirectWithLibreAsync(client, text, sourceProviderCode, targetProviderCode);

        // Guard: if result is effectively unchanged while source/target differ, treat it as translation failure.
        if (LooksUntranslated(text, translated)
            && !string.Equals(sourceProviderCode, targetProviderCode, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(sourceProviderCode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                var guessedSourceUiCode = GuessLikelySourceLanguageCode(text);
                if (!string.IsNullOrWhiteSpace(guessedSourceUiCode)
                    && !TryResolveProviderLanguageCode(guessedSourceUiCode, availableCodes, out _))
                {
                    throw new ArgumentException(
                        $"LibreTranslate container does not have source language '{guessedSourceUiCode}'. " +
                        $"Installed languages: {string.Join(", ", availableCodes.OrderBy(x => x))}.");
                }
            }

            throw new HttpRequestException(
                $"LibreTranslate returned untranslated content for '{sourceProviderCode}' -> '{targetProviderCode}'.");
        }

        return translated;
    }

    private async Task<string> TranslateDirectWithLibreAsync(
        HttpClient client,
        string text,
        string sourceProviderCode,
        string targetProviderCode)
    {
        var endpoint = BuildEndpoint(_libreTranslateSettings.BaseUrl, _libreTranslateSettings.TranslatePath);

        var payload = new
        {
            q = text,
            source = sourceProviderCode,
            target = targetProviderCode,
            format = "text"
        };

        var response = await client.PostAsJsonAsync(endpoint, payload);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"LibreTranslate returned {(int)response.StatusCode}: {body}");
        }

        var content = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>();
        if (content == null || string.IsNullOrWhiteSpace(content.TranslatedText))
        {
            throw new HttpRequestException("LibreTranslate returned empty translatedText.");
        }

        return content.TranslatedText.Trim();
    }

    private async Task<List<LibreLanguageInfo>> GetLibreLanguagesAsync(HttpClient client)
    {
        var endpoint = BuildEndpoint(_libreTranslateSettings.BaseUrl, "/languages");
        var response = await client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to fetch LibreTranslate languages: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var languages = await response.Content.ReadFromJsonAsync<List<LibreLanguageInfo>>();
        return languages ?? [];
    }

    private static bool CanTranslateDirect(
        string sourceProviderCode,
        string targetProviderCode,
        IReadOnlyCollection<LibreLanguageInfo> languages)
    {
        if (string.Equals(sourceProviderCode, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var source = languages.FirstOrDefault(x =>
            string.Equals(x.Code, sourceProviderCode, StringComparison.OrdinalIgnoreCase));
        if (source == null || source.Targets == null || source.Targets.Count == 0)
        {
            return true;
        }

        return source.Targets.Any(x => string.Equals(x, targetProviderCode, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveProviderLanguageCode(
        string uiLanguageCode,
        IReadOnlySet<string> availableCodes,
        bool isTarget)
    {
        if (availableCodes.Count == 0)
        {
            return NormalizeBaseLanguageCode(uiLanguageCode);
        }

        if (TryResolveProviderLanguageCode(uiLanguageCode, availableCodes, out var providerCode))
        {
            return providerCode;
        }

        var role = isTarget ? "target" : "source";
        throw new ArgumentException(
            $"LibreTranslate does not support {role} language '{uiLanguageCode}'. " +
            $"Installed languages: {string.Join(", ", availableCodes.OrderBy(x => x))}.");
    }

    private static bool TryResolveProviderLanguageCode(
        string uiLanguageCode,
        IReadOnlySet<string> availableCodes,
        out string providerCode)
    {
        providerCode = string.Empty;

        foreach (var candidate in GetProviderLanguageCandidates(uiLanguageCode))
        {
            var match = availableCodes.FirstOrDefault(x => string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                providerCode = match;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetProviderLanguageCandidates(string uiLanguageCode)
    {
        var normalized = NormalizeBaseLanguageCode(uiLanguageCode);

        return normalized switch
        {
            "zh" => ["zh", "zh-Hans", "zh-CN", "zh-Hant", "zh-TW"],
            "vi" => ["vi", "vi-VN"],
            "ja" => ["ja", "ja-JP"],
            "ko" => ["ko", "ko-KR"],
            "en" => ["en", "en-US", "en-GB"],
            _ => [normalized]
        };
    }

    private static string GuessLikelySourceLanguageCode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "en";
        }

        // Vietnamese-specific characters.
        const string vietnameseChars = "ăâđêôơưĂÂĐÊÔƠƯáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";
        if (text.Any(c => vietnameseChars.Contains(c)))
        {
            return "vi";
        }

        if (text.Any(c => c >= '\uAC00' && c <= '\uD7AF'))
        {
            return "ko";
        }

        if (text.Any(c => (c >= '\u3040' && c <= '\u30FF') || (c >= '\u31F0' && c <= '\u31FF')))
        {
            return "ja";
        }

        if (text.Any(c => c >= '\u4E00' && c <= '\u9FFF'))
        {
            return "zh";
        }

        return "en";
    }

    private static bool LooksUntranslated(string source, string translated)
    {
        var a = source.Trim();
        var b = translated.Trim();
        if (a.Length == 0 || b.Length == 0)
        {
            return false;
        }

        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<EdgeTtsAudioResult> GenerateAudioWithEdgeTtsAsync(string text, string languageCode, string? voice)
    {
        var endpoint = BuildEndpoint(_edgeTtsSettings.BaseUrl, _edgeTtsSettings.SynthesizePath);
        var client = _httpClientFactory.CreateClient(nameof(TranslationService));
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_edgeTtsSettings.TimeoutSeconds, 5, 180));

        var payload = new
        {
            text,
            language_code = languageCode,
            voice
        };

        var response = await client.PostAsJsonAsync(endpoint, payload);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Edge TTS service returned {(int)response.StatusCode}: {body}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0)
        {
            throw new HttpRequestException("Edge TTS service returned empty audio response.");
        }

        var usedVoice = response.Headers.TryGetValues("x-edge-voice", out var values)
            ? values.FirstOrDefault() ?? string.Empty
            : string.Empty;

        return new EdgeTtsAudioResult
        {
            AudioBytes = bytes,
            Voice = usedVoice
        };
    }

    private decimal CalculateEstimatedCost(int inputChars)
    {
        var rawCost = (inputChars / 1000m) * _translationPricingSettings.PricePer1KChars;
        return Math.Round(rawCost, 6, MidpointRounding.AwayFromZero);
    }

    private static string BuildEndpoint(string baseUrl, string relativePath)
    {
        var root = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost" : baseUrl.TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(relativePath) ? string.Empty : "/" + relativePath.TrimStart('/');
        return root + path;
    }

    private static string NormalizeRequestId(string? requestId)
    {
        return string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId.Trim();
    }

    private static string NormalizeSourceLanguageCode(string? sourceLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(sourceLanguageCode))
        {
            return "auto";
        }

        var normalized = NormalizeBaseLanguageCode(sourceLanguageCode);
        if (string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return "auto";
        }

        if (!UiSupportedLanguageCodes.Contains(normalized))
        {
            throw new ArgumentException($"Unsupported source language '{sourceLanguageCode}'.");
        }

        return normalized;
    }

    private static string NormalizeRequiredLanguageCode(string languageCode)
    {
        var normalized = NormalizeBaseLanguageCode(languageCode);
        if (!UiSupportedLanguageCodes.Contains(normalized))
        {
            throw new ArgumentException($"Unsupported language '{languageCode}'. Allowed: en, vi, ja, zh, ko.");
        }

        return normalized;
    }

    private static string NormalizeBaseLanguageCode(string languageCode)
    {
        var normalized = languageCode.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        if (normalized.Contains('-'))
        {
            normalized = normalized.Split('-', 2)[0];
        }

        return normalized switch
        {
            "vn" => "vi",
            "jp" => "ja",
            "kr" => "ko",
            "cn" => "zh",
            _ => normalized
        };
    }

    private static string ComputeSha256(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }

    private sealed class LibreLanguageInfo
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("targets")]
        public List<string> Targets { get; set; } = [];
    }

    private sealed class EdgeTtsAudioResult
    {
        public byte[] AudioBytes { get; set; } = Array.Empty<byte>();
        public string Voice { get; set; } = string.Empty;
    }
}
