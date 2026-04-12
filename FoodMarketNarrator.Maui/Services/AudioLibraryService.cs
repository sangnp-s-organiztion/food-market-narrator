using System.Text.Json;
using System.Diagnostics;
using food_market_narrator.Models;

namespace food_market_narrator.Services;

public sealed class AudioLibraryService : IAudioLibraryService
{
    private const string AudioReadyPreferenceKey = "audio_ready";
    private const string FirstInstallOfflineNoticeShownKey = "audio_first_install_offline_notice_shown";
    private const string StartupOfflineNoticePendingKey = "audio_startup_offline_notice_pending";
    private const string AudioManifestFileName = "audio_manifest.json";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly IPOIService _poiService;
    private readonly IAudioService _audioService;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    public AudioLibraryService(IPOIService poiService, IAudioService audioService)
    {
        _poiService = poiService;
        _audioService = audioService;
    }


    // hàm này dùng để khởi tạo thư viện audio khi app khởi động. Nó sẽ kiểm tra xem audio đã sẵn sàng chưa (audio_ready), nếu chưa thì sẽ cố gắng đồng bộ tất cả audio có sẵn từ server. Nếu đã sẵn sàng và có kết nối internet, nó sẽ chỉ đồng bộ những phiên bản mới hơn của audio.
    public async Task InitializeOnStartupAsync()
    {
        await _syncGate.WaitAsync();
        try
        {
            var isAudioReady = Preferences.Get(AudioReadyPreferenceKey, false);
            var hasInternet = HasInternetConnection();
            Log($"InitializeOnStartupAsync start. audio_ready={isAudioReady}, hasInternet={hasInternet}");

            if (!isAudioReady)
            {
                if (!hasInternet)
                {
                    Log("First install audio sync blocked: no internet.");
                    QueueFirstInstallOfflineNoticeIfNeeded();
                    return;
                }

                var fullSyncResult = await SyncAllAvailableAudiosAsync();
                Log($"Initial full sync done. total={fullSyncResult.TotalCandidates}, ready={fullSyncResult.DownloadedOrAlreadyLocal}");
                if (fullSyncResult.TotalCandidates > 0 && fullSyncResult.DownloadedOrAlreadyLocal >= fullSyncResult.TotalCandidates)
                {
                    Preferences.Set(AudioReadyPreferenceKey, true);
                    Log("audio_ready=true (initial sync completed).");
                }
                else
                {
                    Log("audio_ready remains false (not enough local audio or no candidate).");
                }

                return;
            }

            if (hasInternet)
            {
                Log("audio_ready=true and internet available, checking newer versions.");
                await SyncOnlyNewerVersionsAsync();
            }
            else
            {
                Log("audio_ready=true and offline, continue using local audio only.");
            }
        }
        catch (Exception ex)
        {
            Log($"InitializeOnStartupAsync failed: {ex.Message}");
        }
        finally
        {
            Log("InitializeOnStartupAsync end.");
            _syncGate.Release();
        }
    }


    // hàm này được dùng để kiểm tra xem có cần hiển thị thông báo offline cho người dùng khi app khởi động hay không. Nếu có, nó sẽ trả về true và đồng thời xóa cờ đã hiển thị để lần sau không hiển thị nữa.
    public bool ConsumeStartupOfflineNoticeFlag()
    {
        var pending = Preferences.Get(StartupOfflineNoticePendingKey, false);
        if (pending)
        {
            Preferences.Set(StartupOfflineNoticePendingKey, false);
        }

        return pending;
    }


    // hàm này được dùng để lấy danh sách tất cả audio có sẵn từ server, bất kể đã có trong thư viện local hay chưa. Nó sẽ trả về một danh sách các audio model với thông tin chi tiết.
    private async Task<SyncProgress> SyncAllAvailableAudiosAsync()
    {
        var pois = await _poiService.GetAllPOIsAsync();
        var audios = FlattenAudioList(pois);
        var manifest = await LoadManifestAsync();
        Log($"SyncAllAvailableAudiosAsync: pois={pois.Count}, audioCandidates={audios.Count}, manifestItems={manifest.Items.Count}");

        var result = new SyncProgress { TotalCandidates = audios.Count };

        foreach (var audio in audios)
        {
            var itemKey = BuildManifestKey(audio.AudioId);

            if (_audioService.HasLocalAudio(audio.AudioId))
            {
                manifest.Items[itemKey] = new AudioManifestItem
                {
                    AudioId = audio.AudioId,
                    LanguageCode = audio.LanguageCode,
                    AudioUrl = audio.AudioUrl,
                    Version = audio.Version,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                result.DownloadedOrAlreadyLocal++;
                Log($"Skip download (already local): audioId={audio.AudioId} | v{audio.Version} | {audio.AudioUrl}");
                continue;
            }

            var ok = await _audioService.PrefetchAudioAsync(audio.AudioId);
            if (!ok)
            {
                Log($"Prefetch failed: audioId={audio.AudioId} | v{audio.Version} | {audio.AudioUrl}");
                continue;
            }

            manifest.Items[itemKey] = new AudioManifestItem
            {
                AudioId = audio.AudioId,
                LanguageCode = audio.LanguageCode,
                AudioUrl = audio.AudioUrl,
                Version = audio.Version,
                UpdatedAtUtc = DateTime.UtcNow
            };
            result.DownloadedOrAlreadyLocal++;
            Log($"Prefetch success: audioId={audio.AudioId} | v{audio.Version} | {audio.AudioUrl}");
        }

        await SaveManifestAsync(manifest);
        Log($"SyncAllAvailableAudiosAsync complete: total={result.TotalCandidates}, ready={result.DownloadedOrAlreadyLocal}");
        return result;
    }


    // hàm này được dùng để kiểm tra và chỉ đồng bộ những audio nào có phiên bản mới hơn so với phiên bản đã có trong thư viện local. Nó sẽ giúp tiết kiệm băng thông và thời gian khi không cần thiết phải tải lại những audio đã có sẵn.
    private async Task SyncOnlyNewerVersionsAsync()
    {
        var pois = await _poiService.GetAllPOIsAsync();
        var audios = FlattenAudioList(pois);
        var manifest = await LoadManifestAsync();
        Log($"SyncOnlyNewerVersionsAsync: pois={pois.Count}, audioCandidates={audios.Count}, manifestItems={manifest.Items.Count}");

        var changed = false;
        var updatedCount = 0;
        foreach (var audio in audios)
        {
            var itemKey = BuildManifestKey(audio.AudioId);
            var hasItem = manifest.Items.TryGetValue(itemKey, out var localItem);
            var hasLocalAudio = _audioService.HasLocalAudio(audio.AudioId);
            var localVersion = hasItem ? localItem!.Version : 0;

            if (hasLocalAudio && hasItem && localVersion >= audio.Version)
            {
                Log($"No update needed: audioId={audio.AudioId} | localV={localVersion} >= serverV={audio.Version} | {audio.AudioUrl}");
                continue;
            }

            var ok = await _audioService.PrefetchAudioAsync(audio.AudioId);
            if (!ok)
            {
                Log($"Version update prefetch failed: audioId={audio.AudioId} | localV={localVersion} -> serverV={audio.Version} | {audio.AudioUrl}");
                continue;
            }

            manifest.Items[itemKey] = new AudioManifestItem
            {
                AudioId = audio.AudioId,
                LanguageCode = audio.LanguageCode,
                AudioUrl = audio.AudioUrl,
                Version = audio.Version,
                UpdatedAtUtc = DateTime.UtcNow
            };
            changed = true;
            updatedCount++;
            Log($"Version updated: audioId={audio.AudioId} | localV={localVersion} -> serverV={audio.Version} | {audio.AudioUrl}");
        }

        if (changed)
        {
            await SaveManifestAsync(manifest);
            Log($"SyncOnlyNewerVersionsAsync complete: updated={updatedCount}");
        }
        else
        {
            Log("SyncOnlyNewerVersionsAsync complete: no updates.");
        }
    }

    // hàm này được dùng để kiểm tra xem thiết bị hiện tại có kết nối internet hay không. Nó sẽ trả về true nếu có kết nối internet, ngược lại trả về false.
    private static bool HasInternetConnection()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }


    // hàm này được dùng để lấy danh sách tất cả audio có sẵn từ server, bất kể đã có trong thư viện local hay chưa. Nó sẽ trả về một danh sách các audio model với thông tin chi tiết.
    private static List<AudioModel> FlattenAudioList(IEnumerable<POI> pois)
    {
        return pois
            .SelectMany(p => p.Audios)
            .Where(a => a.IsActive && a.AudioId > 0 && !string.IsNullOrWhiteSpace(a.AudioUrl) && !string.IsNullOrWhiteSpace(a.LanguageCode))
            .GroupBy(a => BuildManifestKey(a.AudioId))
            .Select(g => g
                .OrderByDescending(a => a.Version)
                .ThenByDescending(a => a.DateGeneration)
                .First())
            .ToList();
    }

    // hàm này được dùng để xây dựng một khóa duy nhất cho mỗi audio dựa trên audioId. Khóa này sẽ được sử dụng để lưu trữ và tra cứu thông tin audio trong manifest.
    private static string BuildManifestKey(int audioId)
    {
        return audioId.ToString();
    }

    // hàm này được dùng để lấy đường dẫn đến file manifest lưu trữ thông tin về các audio đã tải về. File này sẽ được lưu trong thư mục dữ liệu của ứng dụng.
    private static string GetManifestPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, AudioManifestFileName);
    }

    // hàm này được dùng để tải manifest từ file. Nếu file không tồn tại hoặc có lỗi khi đọc, nó sẽ trả về một manifest mới rỗng. Manifest này chứa thông tin về các audio đã tải về, bao gồm audioId, languageCode, audioUrl, version và thời gian cập nhật.
    private static async Task<AudioManifest> LoadManifestAsync()
    {
        var path = GetManifestPath();
        if (!File.Exists(path))
        {
            return new AudioManifest();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync<AudioManifest>(stream, ManifestJsonOptions);
            return manifest ?? new AudioManifest();
        }
        catch
        {
            return new AudioManifest();
        }
    }

    // hàm này được dùng để lưu manifest vào file. Nó sẽ ghi manifest mới vào một file tạm thời trước, sau đó xóa file cũ (nếu có) và đổi tên file tạm thành file chính. Cách làm này giúp tránh tình trạng file bị hỏng nếu có lỗi xảy ra trong quá trình ghi.
    private static async Task SaveManifestAsync(AudioManifest manifest)
    {
        try
        {
            var path = GetManifestPath();
            var tempPath = $"{path}.tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, ManifestJsonOptions);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch
        {
            // best effort only
        }
    }

    // hàm này được dùng để kiểm tra xem có cần hiển thị thông báo offline cho người dùng khi app khởi động hay không. Nếu có, nó sẽ trả về true và đồng thời xóa cờ đã hiển thị để lần sau không hiển thị nữa.
    private static void QueueFirstInstallOfflineNoticeIfNeeded()
    {
        if (Preferences.Get(FirstInstallOfflineNoticeShownKey, false))
        {
            Log("First-install offline notice already shown before, skip.");
            return;
        }

        Preferences.Set(StartupOfflineNoticePendingKey, true);
        Preferences.Set(FirstInstallOfflineNoticeShownKey, true);
        Log("Queued first-install offline notice.");
    }

    // hàm này được dùng để ghi log với tiền tố [AudioLibraryService] để dễ dàng phân biệt trong output debug. Nó sẽ giúp theo dõi quá trình đồng bộ audio và phát hiện lỗi nếu có.
    private static void Log(string message)
    {
        Debug.WriteLine($"[AudioLibraryService] {message}");
    }


    // các lớp phụ trợ để quản lý tiến trình đồng bộ và lưu trữ thông tin manifest của audio. SyncProgress dùng để theo dõi số lượng audio đã xử lý so với tổng số audio cần đồng bộ. AudioManifest và AudioManifestItem dùng để lưu trữ thông tin chi tiết về các audio đã tải về, bao gồm phiên bản và thời gian cập nhật, giúp cho việc kiểm tra và đồng bộ phiên bản mới dễ dàng hơn.
    private sealed class SyncProgress
    {
        public int TotalCandidates { get; set; }
        public int DownloadedOrAlreadyLocal { get; set; }
    }

    // lớp này đại diện cho manifest lưu trữ thông tin về các audio đã tải về. Nó sử dụng một dictionary để lưu trữ các mục manifest, với khóa là một chuỗi duy nhất (dựa trên audioId) và giá trị là một AudioManifestItem chứa thông tin chi tiết về audio đó.
    private sealed class AudioManifest
    {
        public Dictionary<string, AudioManifestItem> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    // lớp này đại diện cho một mục trong manifest, chứa thông tin chi tiết về một audio cụ thể, bao gồm audioId, languageCode, audioUrl, version và thời gian cập nhật. Thông tin này sẽ được sử dụng để kiểm tra phiên bản và đồng bộ audio mới khi cần thiết.
    private sealed class AudioManifestItem
    {
        public int AudioId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
