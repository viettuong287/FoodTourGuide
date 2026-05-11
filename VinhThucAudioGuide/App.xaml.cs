namespace VinhThucAudioGuide;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Khôi phục ngôn ngữ UI: ưu tiên pref_language_code (đồng bộ với ngôn ngữ thuyết minh
        // user đã chọn ở LanguageSelectionPage). Fallback "AppLang" cho tương thích cũ.
        var prefCode = Preferences.Default.Get("pref_language_code", string.Empty);
        if (!string.IsNullOrEmpty(prefCode))
            LocalizationManager.Instance.SetLanguageByCode(prefCode);
        else
            LocalizationManager.Instance.CurrentLanguage = Preferences.Default.Get("AppLang", "Tiếng Việt");

        // Luôn hiển thị Splash trước, SplashPage tự xử lý điều hướng
        MainPage = new SplashPage();

        // Kích hoạt Heartbeat định kỳ 20 giây để Admin thấy thiết bị Online
        StartHeartbeat();
    }

    private void StartHeartbeat()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(20);
        timer.Tick += async (s, e) =>
        {
            try
            {
                var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
                if (apiService != null)
                {
                    await apiService.SendHeartbeatAsync();
                }
            }
            catch { /* Im lặng */ }
        };
        timer.Start();
    }

    protected override async void OnSleep()
    {
        // Khi người dùng thoát App hoặc cho App xuống nền, báo Offline để giảm số lượng trên Web
        var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
        if (apiService != null)
        {
            await apiService.NotifyOfflineAsync();
        }
    }

    protected override async void OnResume()
    {
        // Khi người dùng quay lại App, báo Online ngay lập tức
        var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
        if (apiService != null)
        {
            await apiService.SendHeartbeatAsync();
        }
    }
}