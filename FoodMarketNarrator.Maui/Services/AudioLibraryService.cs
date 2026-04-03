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

    public bool ConsumeStartupOfflineNoticeFlag()
    {
        var pending = Preferences.Get(StartupOfflineNoticePendingKey, false);
        if (pending)
        {
            Preferences.Set(StartupOfflineNoticePendingKey, false);
        }

        return pending;
    }

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

    private static bool HasInternetConnection()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

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

    private static string BuildManifestKey(int audioId)
    {
        return audioId.ToString();
    }

    private static string GetManifestPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, AudioManifestFileName);
    }

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

    private static void Log(string message)
    {
        Debug.WriteLine($"[AudioLibraryService] {message}");
    }

    private sealed class SyncProgress
    {
        public int TotalCandidates { get; set; }
        public int DownloadedOrAlreadyLocal { get; set; }
    }

    private sealed class AudioManifest
    {
        public Dictionary<string, AudioManifestItem> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class AudioManifestItem
    {
        public int AudioId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
