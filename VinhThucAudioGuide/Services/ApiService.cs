using System.Net.Http.Json;
using System.Text.Json;
using VinhThucAudioGuide.Models;

namespace VinhThucAudioGuide.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    // CHÚ Ý QUAN TRỌNG: 
    // 1. Dùng http://10.0.2.2:5299/api/ nếu chạy trên GIẢ LẬP (Emulator)
    // 2. Dùng IP máy tính (VD: http://192.168.1.5:5299/api/) nếu chạy trên MÁY THẬT (Redmi 10C)
    // Bạn hãy thay địa chỉ IP dưới đây cho đúng với IP máy tính của bạn:
    // Địa chỉ máy chủ trên mạng (Azure)
    private const string BaseUrl = "https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/"; 

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
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
    /// Gửi tín hiệu Heartbeat để Server biết thiết bị vẫn đang online (thời gian thực)
    /// </summary>
    public async Task SendHeartbeatAsync(string languageId = null)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            var deviceId = GetOrCreateDeviceId();

            // Lấy languageId mặc định nếu không truyền vào
            if (string.IsNullOrEmpty(languageId))
            {
                languageId = Guid.Empty.ToString();
            }

            var deviceDto = new
            {
                DeviceId = deviceId,
                LanguageId = languageId,
                Platform = DeviceInfo.Current.Platform.ToString(),
                DeviceModel = DeviceInfo.Current.Model,
                Manufacturer = DeviceInfo.Current.Manufacturer,
                OsVersion = DeviceInfo.Current.VersionString,
                SpeechRate = 1.0,
                AutoPlay = true
            };

            var response = await _httpClient.PostAsJsonAsync("device-preference", deviceDto);
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Sent for {deviceId}. Status: {response.StatusCode}");
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Error: {ex.Message}");
            // Chỉ hiện thông báo này khi đang ở màn hình Splash để debug
            if (Application.Current?.MainPage is SplashPage splash)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    await splash.DisplayAlert("Lỗi kết nối", 
                        $"Không thể gửi tín hiệu tới Server tại {BaseUrl}. Lỗi: {ex.Message}. Vui lòng kiểm tra Wifi!", "OK");
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

    private string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            Preferences.Default.Set("UniqueDeviceId", deviceId);
        }
        return deviceId;
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
    public string Description { get; set; }
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
