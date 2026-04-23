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
        "English" => "Welcome to Vinh Thuc. Experience the island's beauty and local stories.",
        "Français" => "Bienvenue à Vinh Thuc. Découvrez la beauté de l'île et ses récits locaux.",
        "中文" => "欢迎来到永实。体验岛屿的美丽和本地故事。",
        "한국어" => "Vinh Thuc에 오신 것을 환영합니다. 섬의 아름다움과 지역 이야기를 체험하세요.",
        _ => "Chào mừng bạn đến với đảo Vĩnh Thực. Hãy trải nghiệm vẻ đẹp hoang sơ và ngọn hải đăng cổ kính."
    };

    // --- TỪ ĐIỂN TRANG CHỦ ---
    public string AppTitle => CurrentLanguage switch { "English" => "Vinh Thuc Guide", "Français" => "Guide Vinh Thuc", "中文" => "永实岛导览", "한국어" => "빈득 안내", _ => "Vĩnh Thực Audio Guide" };
    public string WelcomeText => CurrentLanguage switch { "English" => "Discover island beauty through automated stories.", "Français" => "Découvrez l'île à travers des récits.", "中文" => "通过故事探索岛屿。", "한국어" => "자동 내레이션으로 섬의 아름다움을 발견하세요.", _ => "Khám phá vẻ đẹp biển đảo qua từng câu chuyện kể." };
    public string StartButton => CurrentLanguage switch { "English" => "START", "Français" => "DÉMARRER", "中文" => "开始", "한국어" => "시작", _ => "BẮT ĐẦU" };

    // --- TỪ ĐIỂN TRANG CÀI ĐẶT ---
    public string SettingsTitle => CurrentLanguage switch { "English" => "SETTINGS", "Français" => "PARAMÈTRES", "中文" => "设置", "한국어" => "설정", _ => "CÀI ĐẶT" };
    public string LanguageLabel => CurrentLanguage switch { "English" => "App Language", "Français" => "Langue de l'application", "中文" => "应用语言", "한국어" => "앱 언어", _ => "Ngôn ngữ ứng dụng" };
    public string AutoPlayLabel => CurrentLanguage switch { "English" => "Auto-play Audio", "Français" => "Lecture automatique", "中文" => "自动播放语音", "한국어" => "자동 재생", _ => "Tự động phát âm thanh" };
    public string VoiceSpeedLabel => CurrentLanguage switch { "English" => "Voice Speed", "Français" => "Vitesse de la voix", "中文" => "语音语速", "한국어" => "음성 속도", _ => "Tốc độ đọc" };
    public string SaveButton => CurrentLanguage switch { "English" => "SAVE CHANGES", "Français" => "ENREGISTRER", "中文" => "保存更改", "한국어" => "저장", _ => "LƯU THAY ĐỔI" };

    // --- TỪ ĐIỂN TRANG BẢN ĐỒ ---
    public string MapTitle => CurrentLanguage switch { "English" => "Tourist Map", "Français" => "Carte", "中文" => "地图", "한국어" => "지도", _ => "Bản Đồ Du Lịch" };
    public string ListenButton => CurrentLanguage switch { "English" => "Listen", "Français" => "Écouter", "中文" => "收听", "한국어" => "듣기", _ => "Nghe Thuyết Minh" };
    public string StopButton => CurrentLanguage switch { "English" => "Stop", "Français" => "Arrêter", "中文" => "停止", "한국어" => "중지", _ => "Dừng" };

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}