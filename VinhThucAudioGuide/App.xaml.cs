namespace VinhThucAudioGuide;

public partial class App : Application
{
    public Services.ApiService ApiService => _apiService;
    public Services.LocalDbService LocalDbService => _localDbService;

    private Services.ApiService _apiService;
    private Services.LocalDbService _localDbService;

    public App()
    {
        InitializeComponent();
        LocalizationManager.Instance.CurrentLanguage = Preferences.Default.Get("AppLang", "Tiếng Việt");

        // Khởi tạo service
        _apiService = new Services.ApiService();
        _localDbService = new Services.LocalDbService();

        MainPage = new SplashPage();
    }

    protected override async void OnStart()
    {
        // Khi bắt đầu App, báo Online ngay lập tức
        if (_apiService != null)
        {
            await _apiService.SendHeartbeatAsync();
        }
        
        // Kích hoạt Heartbeat định kỳ 20 giây
        StartHeartbeat();
    }

    private void StartHeartbeat()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(20);
        timer.Tick += async (s, e) =>
        {
            if (_apiService != null)
            {
                await _apiService.SendHeartbeatAsync();
            }
        };
        timer.Start();
    }

    protected override async void OnSleep()
    {
        // Khi ẩn App, báo Offline ngay lập tức
        if (_apiService != null)
        {
            await _apiService.NotifyOfflineAsync();
        }
    }

    protected override async void OnResume()
    {
        // Khi quay lại App, báo Online lại ngay
        if (_apiService != null)
        {
            await _apiService.SendHeartbeatAsync();
        }
    }
}