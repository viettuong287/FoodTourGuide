namespace VinhThucAudioGuide;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        LocalizationManager.Instance.CurrentLanguage = Preferences.Default.Get("AppLang", "Tiếng Việt");

        // Luôn hiển thị Splash trước, SplashPage tự xử lý điều hướng
        MainPage = new SplashPage();
    }
}