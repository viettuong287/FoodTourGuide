namespace VinhThucAudioGuide;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        LocalizationManager.Instance.CurrentLanguage = Preferences.Default.Get("AppLang", "Tiếng Việt");

        // Kiểm tra xem đã được cấp phép chưa
        bool isUnlocked = Preferences.Default.Get("IsAppUnlocked", false);

        if (isUnlocked)
        {
            // Nếu đã quét QR rồi thì vào thẳng giao diện chính (AppShell hoặc MainPage)
            MainPage = new AppShell();
        }
        else
        {
            // Nếu chưa quét thì nhốt ở trang Quét mã QR
            MainPage = new QRScannerPage();
        }
    }
}