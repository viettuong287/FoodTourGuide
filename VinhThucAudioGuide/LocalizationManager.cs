using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VinhThucAudioGuide;

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new LocalizationManager();

    private string _currentLanguage = "Tiếng Việt";
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set { _currentLanguage = value; OnPropertyChanged(); }
    }

    // --- KỊCH BẢN THUYẾT MINH 5 THỨ TIẾNG ---
    public string AudioScript => CurrentLanguage switch
    {
        "English" => "Welcome to Vinh Thuc Island. Experience the pristine beauty and the ancient lighthouse.",
        "Français" => "Bienvenue sur l'île de Vinh Thuc. Découvrez la beauté sauvage et le phare historique.",
        "中文" => "欢迎来到永实岛。在这里，您可以体验原始的美景和古老的灯塔。",
        "日本語" => "ヴィントゥック島へようこそ。手つかずの自然と歴史的な灯台をお楽しみください。",
        _ => "Chào mừng bạn đến với đảo Vĩnh Thực. Hãy trải nghiệm vẻ đẹp hoang sơ và ngọn hải đăng cổ kính."
    };

    // --- TỪ ĐIỂN TRANG CHỦ ---
    public string AppTitle => CurrentLanguage switch { "English" => "Vinh Thuc Guide", "Français" => "Guide Vinh Thuc", "中文" => "永实岛导览", "日本語" => "島ガイド", _ => "Vĩnh Thực Audio Guide" };
    public string WelcomeText => CurrentLanguage switch { "English" => "Discover island beauty through automated stories.", "Français" => "Découvrez l'île à travers des récits.", "中文" => "通过故事探索岛屿。", "日本語" => "物語で島を探索する。", _ => "Khám phá vẻ đẹp biển đảo qua từng câu chuyện kể." };
    public string StartButton => CurrentLanguage switch { "English" => "START", "Français" => "DÉMARRER", "中文" => "开始", "日本語" => "スタート", _ => "BẮT ĐẦU" };

    // --- TỪ ĐIỂN TRANG CÀI ĐẶT ---
    public string SettingsTitle => CurrentLanguage switch { "English" => "SETTINGS", "Français" => "PARAMÈTRES", "中文" => "设置", "日本語" => "設定", _ => "CÀI ĐẶT" };
    public string LanguageLabel => CurrentLanguage switch { "English" => "App Language", "Français" => "Langue de l'application", "中文" => "应用语言", "日本語" => "アプリの言語", _ => "Ngôn ngữ ứng dụng" };
    public string AutoPlayLabel => CurrentLanguage switch { "English" => "Auto-play Audio", "Français" => "Lecture automatique", "中文" => "自动播放语音", "日本語" => "自動再生", _ => "Tự động phát âm thanh" };
    public string VoiceSpeedLabel => CurrentLanguage switch { "English" => "Voice Speed", "Français" => "Vitesse de la voix", "中文" => "语音语速", "日本語" => "音声の速度", _ => "Tốc độ đọc" };
    public string SaveButton => CurrentLanguage switch { "English" => "SAVE CHANGES", "Français" => "ENREGISTRER", "中文" => "保存更改", "日本語" => "変更を保存", _ => "LƯU THAY ĐỔI" };

    // --- TỪ ĐIỂN TRANG BẢN ĐỒ ---
    public string MapTitle => CurrentLanguage switch { "English" => "Tourist Map", "Français" => "Carte", "中文" => "地图", "日本語" => "マップ", _ => "Bản Đồ Du Lịch" };
    public string ListenButton => CurrentLanguage switch { "English" => "Listen", "Français" => "Écouter", "中文" => "收听", "日本語" => "聞く", _ => "Nghe Thuyết Minh" };
    public string StopButton => CurrentLanguage switch { "English" => "Stop", "Français" => "Arrêter", "中文" => "停止", "日本語" => "停止", _ => "Dừng" };

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}