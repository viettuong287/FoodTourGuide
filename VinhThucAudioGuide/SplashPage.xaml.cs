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
        // Hiển thị splash 3 giây rồi vào AppShell (tab Trang chủ đầu tiên)
        await Task.Delay(3000);

        bool isUnlocked = Preferences.Default.Get("IsAppUnlocked", false);
        Application.Current!.MainPage = isUnlocked ? new AppShell() : new QRScannerPage();
    }
}
