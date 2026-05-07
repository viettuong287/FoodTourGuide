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

        var app = (App)Application.Current!;
        var localDb = app.LocalDbService;
        var apiService = app.ApiService;

        var syncTask = Task.Run(async () => 
        {
            if (localDb != null && apiService != null)
            {
                // 1. Kiểm tra lệnh Reset từ Admin
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
