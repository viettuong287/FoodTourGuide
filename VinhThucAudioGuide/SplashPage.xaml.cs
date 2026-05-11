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

        // Hiển thị trạng thái khởi động ban đầu cho người dùng
        await UpdateProgressAsync(0.05, "Đang khởi động ứng dụng...");

        // Toàn bộ phần đồng bộ dữ liệu nặng chạy trong Task riêng để UI không bị block.
        // Các bước bên trong sẽ gọi UpdateProgressAsync (dispatch về MainThread)
        // để cập nhật progress bar theo tiến độ thực tế.
        var syncTask = Task.Run(async () =>
        {
            if (localDb == null || apiService == null) return;

            // Lưu BaseUrl mặc định để các thành phần khác dùng (như /api/mobile/tts).
            // Không ghi đè nếu máy đã cấu hình API riêng.
            Preferences.Default.Set("RemoteApiBase", Services.ApiService.GetConfiguredApiBase());

            await UpdateProgressAsync(0.10, "Đang kết nối máy chủ...");

            // 1a. Kiểm tra phiên bản schema cục bộ — bump phiên bản sẽ buộc app xoá cache
            // để sync lại sạch sẽ dữ liệu mới từ server.
            const int LocalDbSchemaVersion = 4;
            int storedVersion = Preferences.Default.Get("LocalDbSchemaVersion", 0);
            if (storedVersion < LocalDbSchemaVersion)
            {
                await UpdateProgressAsync(0.18, "Đang dọn dữ liệu cũ...");
                await localDb.ClearAllDataAsync();
                Preferences.Default.Remove("LastSyncTime");
                Preferences.Default.Set("LocalDbSchemaVersion", LocalDbSchemaVersion);

                // Xoá luôn file audio đã cache (tts_*.mp3) để không phát nhầm audio cũ.
                try
                {
                    var cacheDir = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
                    if (System.IO.Directory.Exists(cacheDir))
                    {
                        foreach (var f in System.IO.Directory.GetFiles(cacheDir, "tts_*.mp3"))
                        {
                            try { System.IO.File.Delete(f); } catch { /* bỏ qua */ }
                        }
                    }
                }
                catch { /* không chặn flow khởi động */ }
            }

            // 1b. Kiểm tra lệnh Reset từ Admin
            await UpdateProgressAsync(0.25, "Đang kiểm tra cập nhật...");
            bool needsReset = await apiService.CheckResetFlagAsync();
            if (needsReset)
            {
                await UpdateProgressAsync(0.30, "Đang nạp lại toàn bộ dữ liệu...");
                await localDb.ClearAllDataAsync();
                Preferences.Default.Remove("LastSyncTime");
            }

            // 2. Đồng bộ dữ liệu POI — bước nặng nhất, chiếm phần lớn thời gian khởi động.
            // Tiến độ chi tiết được report từ bên trong SyncWithServerAsync qua IProgress.
            // Map vùng 0.35 → 0.85 cho phần sync này.
            var progress = new Progress<(double Percent, string Status)>(p =>
            {
                var mapped = 0.35 + p.Percent * 0.50;
                _ = UpdateProgressAsync(mapped, p.Status);
            });
            await UpdateProgressAsync(0.35, "Đang nạp POI...");
            await localDb.SyncWithServerAsync(apiService, progress);

            // 3. Heartbeat: chỉ gửi nếu user đã chọn ngôn ngữ + giọng trước đó.
            // Nếu CHƯA chọn (lần đầu sau cài đặt) — KHÔNG gửi để tránh ghi đè preference bằng default "vi"
            // và để API tự fallback (cho đến khi user hoàn tất flow LanguageSelectionPage → VoiceSelectionPage).
            await UpdateProgressAsync(0.90, "Đang đồng bộ thiết bị...");
            var savedLangIdHb = Preferences.Default.Get("pref_language_id", string.Empty);
            if (!string.IsNullOrEmpty(savedLangIdHb))
            {
                await apiService.SendHeartbeatAsync();
            }

            await UpdateProgressAsync(0.98, "Đã sẵn sàng");
        });

        // Đảm bảo splash hiển thị tối thiểu 2 giây để người dùng thấy được loading screen
        await Task.WhenAll(Task.Delay(2000), syncTask);
        await UpdateProgressAsync(1.0, "Đã sẵn sàng");

        bool isUnlocked = Preferences.Default.Get("IsAppUnlocked", false);
        if (!isUnlocked)
        {
            // Chưa quét QR → bắt user quét QR trước
            Application.Current!.MainPage = new QRScannerPage();
            return;
        }

        // Đã quét QR. Nếu user cũng đã chọn ngôn ngữ + giọng thì vào thẳng app.
        // Nếu chưa chọn (vd: lần đầu sau khi quét QR) thì đi qua flow
        // LanguageSelectionPage -> VoiceSelectionPage -> AppShell.
        bool hasLanguagePref = !string.IsNullOrEmpty(Preferences.Default.Get("pref_language_id", string.Empty));
        if (hasLanguagePref)
        {
            // Khôi phục ngôn ngữ UI từ pref_language_code (canonical) thay vì DisplayName để
            // tránh trường hợp server trả "Vietnamese" mà LocalizationManager chỉ hiểu "Tiếng Việt".
            var prefCode = Preferences.Default.Get("pref_language_code", string.Empty);
            if (!string.IsNullOrWhiteSpace(prefCode))
                LocalizationManager.Instance.SetLanguageByCode(prefCode);

            Application.Current!.MainPage = new AppShell();
        }
        else
        {
            Application.Current!.MainPage = new NavigationPage(new LanguageSelectionPage());
        }
    }

    /// <summary>
    /// Cập nhật thanh tiến trình + nhãn trạng thái. An toàn để gọi từ background thread —
    /// nội bộ tự dispatch sang MainThread. Dùng ProgressTo để animate mượt thay vì nhảy.
    /// </summary>
    private async Task UpdateProgressAsync(double progress, string status)
    {
        if (progress < 0) progress = 0;
        if (progress > 1) progress = 1;

        var tcs = new TaskCompletionSource<bool>();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                LblStatus.Text = status;
                LblPercent.Text = $"{(int)Math.Round(progress * 100)}%";
                // ProgressTo: animate từ giá trị hiện tại tới target trong 250ms cho mượt
                await PrgLoading.ProgressTo(progress, 250, Easing.CubicInOut);
            }
            catch { /* trang đã unload, bỏ qua */ }
            finally { tcs.TrySetResult(true); }
        });
        await tcs.Task;
    }
}
