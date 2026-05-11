using System.Net.Http.Json;
using System.Text.Json;
using VinhThucAudioGuide.Models;

namespace VinhThucAudioGuide.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // URL Azure đang chạy thật, dùng cho release và cho mọi máy thật chưa override.
    private const string ProductionApiBase = "https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net";

    // URL API local cho emulator Android (10.0.2.2 = localhost của PC dev nhìn từ AVD).
    private const string AndroidEmulatorApiBase = "http://10.0.2.2:5299";

    // URL API local cho Windows / iOS simulator / Mac Catalyst chạy trên cùng máy dev.
    private const string LocalhostApiBase = "http://localhost:5299";

    public ApiService()
    {
        var baseUrl = GetConfiguredApiBase();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{baseUrl}/api/"),
            // 30s để chịu được Azure cold-start (App Service sleep mất 15-25s đánh thức).
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<SyncDataResponse> GetSyncDataAsync(DateTimeOffset? lastSync = null)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return null;
            }

            var url = "audiocontent/sync";
            if (lastSync.HasValue)
            {
                url += $"?lastSync={Uri.EscapeDataString(lastSync.Value.ToString("O"))}";
            }

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<SyncDataResponse>(options);
            }
            else
            {
                // Báo lỗi ra Console để lập trình viên biết (404, 500, v.v...)
                Console.WriteLine($"API Sync Failed: {response.StatusCode} at {url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data from API: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Đăng ký hoặc cập nhật thông tin thiết bị lên Server để Admin quản lý
    /// </summary>
    public async Task RegisterDeviceAsync(string languageId)
    {
        await SendHeartbeatAsync(languageId);
    }

    /// <summary>
    /// Gửi tín hiệu Heartbeat để Server biết thiết bị vẫn đang online (thời gian thực).
    /// QUAN TRỌNG: Luôn ưu tiên pref_language_id / pref_voice_id mà user đã chọn trong
    /// LanguageSelectionPage/VoiceSelectionPage, tránh ghi đè bằng default language ("vi")
    /// mỗi lần app khởi động — vốn là nguyên nhân khiến POI tự đọc tiếng Việt.
    /// Nếu chưa có pref nào và parameter không có → bỏ qua POST hoàn toàn để giữ preference cũ trên server.
    /// </summary>
    public async Task SendHeartbeatAsync(string languageId = null, bool? autoPlay = null, double? speechRate = null)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            var deviceId = GetOrCreateDeviceId();

            // Ưu tiên ngôn ngữ + giọng user đã chọn — KHÔNG cho phép default language overwrite.
            var savedLang = Preferences.Default.Get("pref_language_id", string.Empty);
            if (!string.IsNullOrEmpty(savedLang))
                languageId = savedLang;

            var savedVoice = Preferences.Default.Get("pref_voice_id", string.Empty);
            Guid? voiceId = Guid.TryParse(savedVoice, out var vGuid) ? vGuid : null;

            // Không có info gì hợp lệ → bỏ qua POST, server giữ nguyên preference cũ.
            if (string.IsNullOrEmpty(languageId) || languageId == Guid.Empty.ToString())
            {
                System.Diagnostics.Debug.WriteLine($"[Heartbeat] Skipped for {deviceId} (no language pref yet).");
                return;
            }

            var deviceDto = new
            {
                DeviceId = deviceId,
                LanguageId = languageId,
                VoiceId = voiceId,
                Platform = DeviceInfo.Current.Platform.ToString(),
                DeviceModel = DeviceInfo.Current.Model,
                Manufacturer = DeviceInfo.Current.Manufacturer,
                OsVersion = DeviceInfo.Current.VersionString,
                SpeechRate = speechRate ?? Preferences.Default.Get("VoiceSpeed", 1.0),
                AutoPlay = autoPlay ?? Preferences.Default.Get("AutoPlay", true)
            };

            var response = await _httpClient.PostAsJsonAsync("device-preference", deviceDto);
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Sent for {deviceId} (lang={languageId}, voice={voiceId}). Status: {response.StatusCode}");
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Error: {ex.Message}");
            // Chỉ hiện thông báo này khi đang ở màn hình Splash để debug
            if (Application.Current?.MainPage is SplashPage splash)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    await splash.DisplayAlert("Lỗi kết nối", 
                        $"Không thể gửi tín hiệu tới Server tại {_httpClient.BaseAddress}. Lỗi: {ex.Message}. Vui lòng kiểm tra Wifi!", "OK");
                });
            }
        }
    }

    /// <summary>
    /// Thông báo cho Server biết thiết bị đã thoát App (để giảm số lượng Online ngay lập tức)
    /// </summary>
    public async Task NotifyOfflineAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
            if (string.IsNullOrEmpty(deviceId)) return;

            // Timeout ngắn để tránh treo app khi OnSleep
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _httpClient.PostAsync($"device-preference/{deviceId}/offline", null, cts.Token);
        }
        catch { }
    }

    /// <summary>
    /// Kiểm tra xem Admin có yêu cầu thiết bị này Reset dữ liệu hay không
    /// </summary>
    public async Task<bool> CheckResetFlagAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

            var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
            if (string.IsNullOrEmpty(deviceId)) return false;

            var response = await _httpClient.GetAsync($"device-preference/reset-flag?deviceId={deviceId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<bool>>();
                return result?.Data ?? false;
            }
        }
        catch { }
        return false;
    }

    public async Task<List<SyncVoiceProfile>> GetVoicesAsync(string languageId)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return new();

            var response = await _httpClient.GetAsync($"tts-voice-profiles/active?languageId={languageId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<List<SyncVoiceProfile>>>();
                return result?.Data ?? new();
            }
        }
        catch { }
        return new();
    }

    /// <summary>
    /// Xác thực mã QR vé tham quan qua Server
    /// </summary>
    public async Task<QrVerifyData> VerifyQrCodeAsync(string code)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
            var body = new { code };
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await _httpClient.PostAsJsonAsync("qrcodes/verify", body);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<QrVerifyData>>(options);
                return result?.Data;
            }
        }
        catch (Exception ex) { Console.WriteLine($"[VerifyQR] {ex.Message}"); }
        return null;
    }

    /// <summary>
    /// Lấy danh sách ngôn ngữ đang active từ Server
    /// </summary>
    public async Task<List<ActiveLanguage>> GetActiveLanguagesAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return new();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await _httpClient.GetAsync("languages/active");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<List<ActiveLanguage>>>(options);
                return result?.Data ?? new();
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Languages] {ex.Message}"); }
        return new();
    }

    /// <summary>
    /// Lấy cấu hình thiết bị đã lưu trên Server
    /// </summary>
    public async Task<DevicePreference> GetDevicePreferenceAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
            var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
            if (string.IsNullOrEmpty(deviceId)) return null;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await _httpClient.GetAsync($"device-preference/{deviceId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreference>>(options);
                return result?.Data;
            }
        }
        catch (Exception ex) { Console.WriteLine($"[DevicePref] {ex.Message}"); }
        return null;
    }

    /// <summary>
    /// Lấy danh sách gian hàng (stalls) cho bản đồ
    /// </summary>
    public async Task<List<StallData>> GetStallsAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
            var deviceId = GetOrCreateDeviceId();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await _httpClient.GetAsync($"geo/stalls?deviceId={deviceId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<List<StallData>>>(options);
                return result?.Data;
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Stalls] {ex.Message}"); }
        return null;
    }

    /// <summary>
    /// Lấy thông tin narration (script + audioUrl) cho một stall + ngôn ngữ cụ thể.
    /// Mobile gọi khi user bấm "Nghe thuyết minh" và đã chọn ngôn ngữ.
    /// Trả về JSON nhẹ, không stream nhị phân. Mobile sẽ tự download AudioUrl.
    /// </summary>
    public async Task<MobileNarrationInfo?> GetNarrationAsync(string stallId, string langCode, Guid? voiceId = null, CancellationToken ct = default)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
            if (string.IsNullOrWhiteSpace(stallId) || string.IsNullOrWhiteSpace(langCode)) return null;

            var url = $"mobile/narration?stallId={Uri.EscapeDataString(stallId)}&lang={Uri.EscapeDataString(langCode)}";
            if (voiceId.HasValue) url += $"&voiceId={voiceId.Value}";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MobileNarrationInfo>(options, ct);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Narration] Status {response.StatusCode} for {url}");
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Narration] {ex.Message}"); }
        return null;
    }

    /// <summary>
    /// Tải file audio từ AudioUrl (Blob Storage). Có timeout riêng và không dùng BaseAddress
    /// để cho phép URL tuyệt đối tới blob.
    /// </summary>
    public static async Task<byte[]?> DownloadAudioAsync(string audioUrl, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(audioUrl)) return null;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var bytes = await client.GetByteArrayAsync(audioUrl, ct);
            return bytes;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DownloadAudio] {ex.Message}"); }
        return null;
    }

    /// <summary>
    /// Gửi batch tọa độ GPS lên Server mỗi 20 giây
    /// </summary>
    public async Task SendLocationBatchAsync(List<LocationLogEntry> logs)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
            if (logs == null || logs.Count == 0) return;
            var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
            if (string.IsNullOrEmpty(deviceId)) return;
            var body = new { deviceId, logs };
            await _httpClient.PostAsJsonAsync("device-location-log/batch", body);
        }
        catch (Exception ex) { Console.WriteLine($"[LocationBatch] {ex.Message}"); }
    }

    /// <summary>
    /// Lấy DeviceId duy nhất của thiết bị. Nếu chưa có trong Preferences thì sinh mới Guid và lưu lại.
    /// Public static để các page (chọn ngôn ngữ / chọn giọng) cũng dùng được khi POST device-preference.
    /// </summary>
    public static string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            Preferences.Default.Set("UniqueDeviceId", deviceId);
        }
        return deviceId;
    }

    /// <summary>
    /// POST /api/device-preference để lưu lựa chọn ngôn ngữ + giọng đọc của thiết bị lên server.
    /// Dùng cho luồng "chọn ngôn ngữ → chọn giọng → vào app" sau khi quét QR.
    /// </summary>
    /// <param name="languageId">Guid LanguageId trên server (lấy từ /languages/active).</param>
    /// <param name="voiceId">Guid voice profile (có thể null nếu user không chọn giọng cụ thể).</param>
    /// <returns>true nếu API trả về 2xx; ngược lại false.</returns>
    public async Task<bool> SaveDevicePreferenceAsync(string languageId, Guid? voiceId)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
            if (string.IsNullOrWhiteSpace(languageId)) return false;

            var deviceId = GetOrCreateDeviceId();
            var body = new
            {
                DeviceId = deviceId,
                LanguageId = languageId,
                VoiceId = voiceId,
                SpeechRate = Preferences.Default.Get("VoiceSpeed", 1.0),
                AutoPlay = Preferences.Default.Get("AutoPlay", true),
                Platform = DeviceInfo.Current.Platform.ToString(),
                DeviceModel = DeviceInfo.Current.Model,
                Manufacturer = DeviceInfo.Current.Manufacturer,
                OsVersion = DeviceInfo.Current.VersionString
            };

            var response = await _httpClient.PostAsJsonAsync("device-preference", body);
            System.Diagnostics.Debug.WriteLine($"[DevicePref] Save status: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DevicePref] Save error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// URL API mặc định cho thiết bị hiện tại — tự chọn theo platform + emulator/máy thật.
    /// - Android emulator → 10.0.2.2 (loopback của PC dev nhìn từ AVD)
    /// - Android máy thật → Azure (vì máy thật KHÔNG reach được localhost của PC qua USB)
    /// - Windows/iOS sim → localhost (chạy chung máy dev với API)
    /// - Release → Azure
    /// </summary>
    public static string DefaultApiBase
    {
        get
        {
#if !DEBUG
            return ProductionApiBase;
#else
            try
            {
                var platform = DeviceInfo.Current.Platform;
                var isVirtual = DeviceInfo.Current.DeviceType == DeviceType.Virtual;

                if (platform == DevicePlatform.Android)
                    return isVirtual ? AndroidEmulatorApiBase : ProductionApiBase;

                if (platform == DevicePlatform.iOS)
                    return isVirtual ? LocalhostApiBase : ProductionApiBase;

                return LocalhostApiBase;
            }
            catch
            {
                return ProductionApiBase;
            }
#endif
        }
    }

    public static string GetConfiguredApiBase()
    {
        var configured = Preferences.Default.Get("RemoteApiBase", string.Empty).TrimEnd('/');

        // Khi user đã set IP LAN riêng (vd http://192.168.1.5:5299) thì luôn tôn trọng.
        // Coi giá trị "rác" cũ (rỗng / Azure cũ / 10.0.2.2 mà máy không phải emulator)
        // là chưa cấu hình, để rơi về DefaultApiBase phù hợp với thiết bị hiện tại.
        if (string.IsNullOrWhiteSpace(configured))
            return DefaultApiBase;

        if (configured.Equals(AndroidEmulatorApiBase, StringComparison.OrdinalIgnoreCase)
            && DeviceInfo.Current.Platform == DevicePlatform.Android
            && DeviceInfo.Current.DeviceType != DeviceType.Virtual)
        {
            return DefaultApiBase;
        }

        return configured;
    }
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
}

public class SyncDataResponse
{
    public List<SyncLocation> Locations { get; set; } = new();
    public List<SyncLanguage> Languages { get; set; } = new();
    public List<SyncScript> Scripts { get; set; } = new();
}

public class SyncLocation
{
    public string ServerId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string ImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
}

public class SyncVoiceProfile
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public bool IsDefault { get; set; }
}

public class SyncLanguage
{
    public string ServerId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    // Mã cờ ISO-2 lowercase, vd: vn, us, fr, jp, kr, cn — dùng để mobile hiển thị emoji 🇻🇳
    public string FlagCode { get; set; }
}

public class SyncScript
{
    public string ServerId { get; set; }
    public string LocationId { get; set; }
    public string LanguageId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsActive { get; set; }
}

public class QrVerifyData
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public DateTimeOffset? ExpiryAt { get; set; }
}

public class ActiveLanguage
{
    public string Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string FlagCode { get; set; }
    public bool IsActive { get; set; }
}

public class DevicePreference
{
    public string LanguageId { get; set; }
    public string VoiceId { get; set; }
    public double SpeechRate { get; set; }
    public bool AutoPlay { get; set; }
    public string LanguageCode { get; set; }
    public string LanguageName { get; set; }
}

public class StallData
{
    public string StallId { get; set; }
    public string StallName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; }
    public StallNarration NarrationContent { get; set; }
    public List<StallImage> MediaImages { get; set; } = [];
}

public class StallNarration
{
    public string Id { get; set; }
    public string LanguageId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ScriptText { get; set; }
    public string AudioUrl { get; set; }
}

public class StallImage
{
    public string Url { get; set; }
    public string Caption { get; set; }
    public bool HasCaption { get; set; }
}

public class LocationLogEntry
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

/// <summary>
/// DTO mobile-side cho phản hồi của endpoint /api/mobile/narration.
/// Bao gồm cả script (để hiển thị / fallback TTS) và audioUrl (để phát trực tiếp).
/// </summary>
public class MobileNarrationInfo
{
    public string? NarrationContentId { get; set; }
    public string? StallId { get; set; }
    // LanguageId = ngôn ngữ người dùng yêu cầu
    // SourceLanguageId = ngôn ngữ thật sự của ScriptText (có thể là tiếng Việt gốc)
    // Mobile dùng cặp này để biết có thể đưa ScriptText cho Google/MAUI TTS hay không.
    public string? LanguageId { get; set; }
    public string? SourceLanguageId { get; set; }
    public string? LanguageCode { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ScriptText { get; set; }
    public string? AudioUrl { get; set; }
    public string? AudioId { get; set; }
    public string? TtsVoiceProfileId { get; set; }
    public bool IsTts { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
