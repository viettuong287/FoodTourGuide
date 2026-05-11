namespace VinhThucAudioGuide;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var localDb = IPlatformApplication.Current?.Services.GetService<Services.LocalDbService>();
        var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();

        var syncTask = Task.Run(async () => 
        {
            if (localDb != null && apiService != null)
            {
                // Lưu BaseUrl mặc định để các thành phần khác dùng (như /api/mobile/tts).
                // Không ghi đè nếu máy đã cấu hình API riêng, tránh app trỏ về Azure
                // trong khi web/API đang chạy local với audio mới cập nhật.
                Preferences.Default.Set("RemoteApiBase", Services.ApiService.GetConfiguredApiBase());

                // 1a. Kiểm tra phiên bản schema cục bộ — nếu phiên bản đã thay đổi
                // (vd: lần đầu cài bản mới này, sau khi server xoá toàn bộ POI cũ)
                // thì xoá DB cục bộ để app sync lại đúng dữ liệu mới, tránh hiển thị POI cũ.
                // v4: build cũ lỡ lưu script tiếng Việt vào key ngôn ngữ ngoại trong local DB,
                // bump để xoá local cache giúp app sync lại sạch sẽ từ server.
                const int LocalDbSchemaVersion = 4;
                int storedVersion = Preferences.Default.Get("LocalDbSchemaVersion", 0);
                if (storedVersion < LocalDbSchemaVersion)
                {
                    await localDb.ClearAllDataAsync();
                    Preferences.Default.Remove("LastSyncTime");
                    Preferences.Default.Set("LocalDbSchemaVersion", LocalDbSchemaVersion);

                    // Xoá luôn file audio đã cache (tts_*.mp3) để không phát nhầm audio cũ
                    // (audio cũ có thể bị "lơ lớ" do được sinh trước khi sửa logic translation).
                    try
                    {
                        var cacheDir = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
                        if (System.IO.Directory.Exists(cacheDir))
                        {
                            foreach (var f in System.IO.Directory.GetFiles(cacheDir, "tts_*.mp3"))
                            {
                                try { System.IO.File.Delete(f); } catch { /* bỏ qua nếu không xoá được */ }
                            }
                        }
                    }
                    catch { /* không chặn flow khởi động vì lỗi xoá cache */ }
                }

                // 1b. Kiểm tra lệnh Reset từ Admin
                bool needsReset = await apiService.CheckResetFlagAsync();
                if (needsReset)
                {
                    // Nếu Admin yêu cầu, xóa toàn bộ dữ liệu cũ để tải lại từ đầu
                    await localDb.ClearAllDataAsync();
                    Preferences.Default.Remove("LastSyncTime");
                }

                // 2. Đồng bộ dữ liệu POI
                await localDb.SyncWithServerAsync(apiService);

                // 3. Đăng ký thông tin thiết bị lên Server để Admin quản lý
                var languages = await localDb.GetAllLanguages();
                var defaultLangId = languages.FirstOrDefault()?.ServerId ?? Guid.Empty.ToString();
                await apiService.RegisterDeviceAsync(defaultLangId);
            }
        });

        // Đảm bảo hiển thị splash ít nhất 3 giây
        await Task.WhenAll(Task.Delay(3000), syncTask);

        bool isUnlocked = Preferences.Default.Get("IsAppUnlocked", false);
        Application.Current!.MainPage = isUnlocked ? new AppShell() : new QRScannerPage();
    }
}
