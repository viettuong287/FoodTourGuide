using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using ZXing.Net.Maui;

namespace VinhThucAudioGuide;

/// <summary>
/// Cấu hình camera ZXing dùng chung cho QRScannerPage (lock screen) và QrPage (POI scanner).
/// Cả hai chỉ cần đọc QR, không cần barcode khác → tránh trùng setup ở 2 chỗ.
/// </summary>
internal static class QrCameraOptions
{
    public static BarcodeReaderOptions Default { get; } = new BarcodeReaderOptions
    {
        Formats = BarcodeFormat.QrCode,
        AutoRotate = true,
        Multiple = false
    };
}

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new LocalizationManager();

    private string _currentLanguage = "Tiếng Việt";
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(string.Empty); // refresh all dependent localized properties
        }
    }

    /// <summary>
    /// Đặt ngôn ngữ UI theo language code chuẩn (vi/en/fr/zh/ko/...).
    /// Dùng thay cho việc gán CurrentLanguage trực tiếp vì server có thể trả DisplayName
    /// không khớp (vd "Vietnamese" thay vì "Tiếng Việt") khiến UI fallback về VI.
    /// </summary>
    public void SetLanguageByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        var lower = code.ToLowerInvariant();
        CurrentLanguage = lower switch
        {
            "en" or "en-us" or "en-gb" => "English",
            "fr" or "fr-fr" => "Français",
            "zh" or "zh-cn" or "zh-tw" or "zh-hans" or "zh-hant" => "中文",
            "ko" or "ko-kr" => "한국어",
            _ => "Tiếng Việt"
        };
    }

    /// <summary>Tra cờ emoji ứng với language code chuẩn.</summary>
    public static string GetFlagByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "🌐";
        var lower = code.ToLowerInvariant();
        return lower switch
        {
            "en" or "en-us" => "🇺🇸",
            "en-gb" => "🇬🇧",
            "fr" or "fr-fr" => "🇫🇷",
            "zh" or "zh-cn" or "zh-hans" => "🇨🇳",
            "zh-tw" or "zh-hant" => "🇹🇼",
            "ko" or "ko-kr" => "🇰🇷",
            "vi" or "vi-vn" => "🇻🇳",
            _ => "🌐"
        };
    }

    // --- KỊCH BẢN THUYẾT MINH 5 THỨ TIẾNG ---
    public string AudioScript => CurrentLanguage switch
    {
        "English" => "Welcome to Ho Chi Minh City. Explore the vibrant culture, historic landmarks, and iconic streets of this dynamic metropolis.",
        "Français" => "Bienvenue à Hô-Chi-Minh-Ville. Explorez la culture vibrante, les sites historiques et les rues emblématiques de cette métropole dynamique.",
        "中文" => "欢迎来到胡志明市。探索这座充满活力的大都市的文化、历史地标和标志性街道。",
        "한국어" => "호치민 시에 오신 것을 환영합니다. 이 활기찬 도시의 문화, 역사적 명소, 상징적인 거리를 탐험하세요.",
        _ => "Chào mừng đến với Thành phố Hồ Chí Minh. Hãy khám phá văn hóa sôi động, di tích lịch sử và những con phố nổi tiếng của thành phố năng động này."
    };

    // --- HOME ---
    public string AppTitle => CurrentLanguage switch { "English" => "Ho Chi Minh City Guide", "Français" => "Guide Hô-Chi-Minh", "中文" => "胡志明市导览", "한국어" => "호치민 시 가이드", _ => "Audio Guide Thành phố Hồ Chí Minh" };
    public string WelcomeText => CurrentLanguage switch { "English" => "Explore the vibrant streets, rich history and iconic landmarks of Ho Chi Minh City.", "Français" => "Découvrez les rues animées, l'histoire riche et les sites emblématiques de Hô-Chi-Minh-Ville.", "中文" => "探索胡志明市充满活力的街道、丰富的历史和标志性景点。", "한국어" => "호치민 시의 활기찬 거리, 풍부한 역사, 상징적인 명소를 탐험하세요.", _ => "Khám phá những con phố sôi động, lịch sử phong phú và các địa danh nổi tiếng của Thành phố Hồ Chí Minh." };
    public string StartButton => CurrentLanguage switch { "English" => "EXPLORE THE CITY", "Français" => "EXPLORER LA VILLE", "中文" => "探索城市", "한국어" => "도시 탐험하기", _ => "KHÁM PHÁ THÀNH PHỐ" };

    // --- SETTINGS ---
    public string SettingsTitle => CurrentLanguage switch { "English" => "SETTINGS", "Français" => "PARAMÈTRES", "中文" => "设置", "한국어" => "설정", _ => "CÀI ĐẶT" };
    public string LanguageLabel => CurrentLanguage switch { "English" => "App Language", "Français" => "Langue de l'application", "中文" => "应用语言", "한국어" => "앱 언어", _ => "Ngôn ngữ ứng dụng" };
    public string AutoPlayLabel => CurrentLanguage switch { "English" => "Auto-play Audio", "Français" => "Lecture automatique", "中文" => "自动播放语音", "한국어" => "자동 재생", _ => "Tự động phát âm thanh" };
    public string VoiceSpeedLabel => CurrentLanguage switch { "English" => "Voice Speed", "Français" => "Vitesse de la voix", "中文" => "语音语速", "한국어" => "음성 속도", _ => "Tốc độ đọc" };
    public string SaveButton => CurrentLanguage switch { "English" => "SAVE CHANGES", "Français" => "ENREGISTRER", "中文" => "保存更改", "한국어" => "저장", _ => "LƯU THAY ĐỔI" };
    public string CheckUpdateButton => CurrentLanguage switch { "English" => "Check for updates", "Français" => "Vérifier les mises à jour", "中文" => "检查更新", "한국어" => "업데이트 확인", _ => "Cập nhật mới" };
    public string UpdatingButton => CurrentLanguage switch { "English" => "Checking...", "Français" => "Vérification...", "中文" => "检查中...", "한국어" => "확인 중...", _ => "Đang kiểm tra..." };
    public string GroupLanguage => CurrentLanguage switch { "English" => "LANGUAGE", "Français" => "LANGUE", "中文" => "语言", "한국어" => "언어", _ => "NGÔN NGỮ" };
    public string GroupAudio => CurrentLanguage switch { "English" => "AUDIO & NARRATION", "Français" => "AUDIO & NARRATION", "中文" => "音频和讲解", "한국어" => "오디오 및 내레이션", _ => "ÂM THANH & THUYẾT MINH" };
    public string ChooseLanguageTitle => CurrentLanguage switch { "English" => "Choose language", "Français" => "Choisir la langue", "中文" => "选择语言", "한국어" => "언어 선택", _ => "Chọn ngôn ngữ" };
    public string SaveSuccessMessage => CurrentLanguage switch { "English" => "Settings saved successfully!", "Français" => "Paramètres enregistrés avec succès!", "中文" => "设置已保存！", "한국어" => "설정이 저장되었습니다!", _ => "Lưu cài đặt thành công!" };
    public string UpdateSuccessMessage(int added) => CurrentLanguage switch
    {
        "English" => $"Sync completed. Added {added} new places.",
        "Français" => $"Synchronisation terminée. {added} nouveaux lieux ajoutés.",
        "中文" => $"同步完成。已新增 {added} 个地点。",
        "한국어" => $"동기화 완료. 새 장소 {added}개가 추가되었습니다.",
        _ => $"Đã đồng bộ xong. Thêm {added} địa điểm mới."
    };
    public string ErrorTitle => CurrentLanguage switch { "English" => "Error", "Français" => "Erreur", "中文" => "错误", "한국어" => "오류", _ => "Lỗi" };
    public string SuccessTitle => CurrentLanguage switch { "English" => "Success", "Français" => "Succès", "中文" => "成功", "한국어" => "성공", _ => "Thành công" };
    public string UpdateTitle => CurrentLanguage switch { "English" => "Update", "Français" => "Mise à jour", "中文" => "更新", "한국어" => "업데이트", _ => "Cập nhật" };
    public string OkButton => CurrentLanguage switch { "English" => "OK", "Français" => "OK", "中文" => "确定", "한국어" => "확인", _ => "OK" };

    // --- ONBOARDING (chọn ngôn ngữ + giọng đọc) ---
    public string OnboardingLanguageTitle => CurrentLanguage switch { "English" => "Choose narration language", "Français" => "Choisir la langue de narration", "中文" => "选择讲解语言", "한국어" => "내레이션 언어 선택", _ => "Chọn ngôn ngữ thuyết minh" };
    public string OnboardingLanguageSubtitle => CurrentLanguage switch { "English" => "Pick the language you want to hear. Next you will choose a voice.", "Français" => "Choisissez la langue à écouter. Vous choisirez ensuite une voix.", "中文" => "选择你想要听的语言。下一步将选择朗读声音。", "한국어" => "원하는 언어를 선택하세요. 다음 단계에서 음성을 선택합니다.", _ => "Hãy chọn ngôn ngữ bạn muốn nghe. Bước sau bạn sẽ chọn giọng đọc." };
    public string OnboardingVoiceTitle => CurrentLanguage switch { "English" => "Choose a voice", "Français" => "Choisir une voix", "中文" => "选择朗读声音", "한국어" => "음성 선택", _ => "Chọn giọng đọc" };
    public string OnboardingVoiceDefaultLabel => CurrentLanguage switch { "English" => "Default", "Français" => "Par défaut", "中文" => "默认", "한국어" => "기본", _ => "Mặc định" };
    public string OnboardingVoiceSubtitle => CurrentLanguage switch { "English" => "Voice", "Français" => "Voix", "中文" => "声音", "한국어" => "음성", _ => "Giọng đọc" };
    public string OnboardingSkipVoiceButton => CurrentLanguage switch { "English" => "Skip, use default voice", "Français" => "Passer, utiliser la voix par défaut", "中文" => "跳过，使用默认声音", "한국어" => "건너뛰기, 기본 음성 사용", _ => "Bỏ qua, dùng giọng mặc định" };
    public string OnboardingRetryButton => CurrentLanguage switch { "English" => "Retry", "Français" => "Réessayer", "中文" => "重试", "한국어" => "다시 시도", _ => "Tải lại" };
    public string OnboardingNoLanguages => CurrentLanguage switch { "English" => "Could not load language list.", "Français" => "Impossible de charger la liste des langues.", "中文" => "无法加载语言列表。", "한국어" => "언어 목록을 불러올 수 없습니다.", _ => "Không tải được danh sách ngôn ngữ." };
    public string OnboardingCheckNetwork => CurrentLanguage switch { "English" => "Check your network connection and try again.", "Français" => "Vérifiez votre connexion réseau et réessayez.", "中文" => "请检查网络连接后重试。", "한국어" => "네트워크 연결을 확인하고 다시 시도하세요.", _ => "Kiểm tra kết nối mạng và thử lại." };
    public string OnboardingNoVoices => CurrentLanguage switch { "English" => "No voices available for this language.", "Français" => "Aucune voix disponible pour cette langue.", "中文" => "该语言暂无可用声音。", "한국어" => "이 언어에 사용할 수 있는 음성이 없습니다.", _ => "Chưa có giọng đọc cho ngôn ngữ này." };
    public string OnboardingSkipHint => CurrentLanguage switch { "English" => "Tap Skip to use the default voice.", "Français" => "Touchez Passer pour utiliser la voix par défaut.", "中文" => "点击“跳过”使用默认声音。", "한국어" => "기본 음성을 사용하려면 건너뛰기를 누르세요.", _ => "Bấm Bỏ qua để dùng giọng mặc định." };
    public string OnboardingSaving => CurrentLanguage switch { "English" => "Saving your choice...", "Français" => "Enregistrement de votre choix...", "中文" => "正在保存选择...", "한국어" => "선택 저장 중...", _ => "Đang lưu lựa chọn..." };

    // --- MAP LOADING / EMPTY STATE ---
    public string MapLoadingPois => CurrentLanguage switch { "English" => "Loading places...", "Français" => "Chargement des lieux...", "中文" => "正在加载地点...", "한국어" => "장소 불러오는 중...", _ => "Đang nạp địa điểm..." };
    public string MapEmpty => CurrentLanguage switch { "English" => "No places yet.", "Français" => "Aucun lieu pour le moment.", "中文" => "暂无地点。", "한국어" => "아직 등록된 장소가 없습니다.", _ => "Chưa có địa điểm nào." };
    public string PoiMissingAudioTitle => CurrentLanguage switch { "English" => "No narration", "Français" => "Pas de narration", "中文" => "暂无讲解", "한국어" => "안내 없음", _ => "Chưa có thuyết minh" };
    public string PoiMissingAudioMessage => CurrentLanguage switch { "English" => "This place has no narration audio yet. Please add an audio URL on the admin web and try again.", "Français" => "Ce lieu n'a pas encore de narration audio. Veuillez ajouter une URL audio dans l'admin web puis réessayer.", "中文" => "该地点暂无讲解音频。请在管理后台添加音频 URL 后重试。", "한국어" => "이 장소에는 안내 오디오가 없습니다. 관리 페이지에서 오디오 URL을 추가한 뒤 다시 시도하세요.", _ => "Địa điểm này chưa có audio thuyết minh. Vui lòng tạo Audio URL trên trang quản trị web rồi thử lại." };
    public string BackButton => CurrentLanguage switch { "English" => "Back", "Français" => "Retour", "中文" => "返回", "한국어" => "뒤로", _ => "Quay lại" };
    public string CloseButton => CurrentLanguage switch { "English" => "Close", "Français" => "Fermer", "中文" => "关闭", "한국어" => "닫기", _ => "Đóng" };
    public string ExitAppTitle => CurrentLanguage switch { "English" => "Exit app", "Français" => "Quitter l'app", "中文" => "退出应用", "한국어" => "앱 종료", _ => "Thoát ứng dụng" };
    public string ExitAppMessage => CurrentLanguage switch { "English" => "Are you sure you want to exit?", "Français" => "Voulez-vous vraiment quitter?", "中文" => "确定要退出吗？", "한국어" => "정말 종료하시겠습니까?", _ => "Bạn có chắc muốn thoát?" };
    public string YesButton => CurrentLanguage switch { "English" => "Yes", "Français" => "Oui", "中文" => "是", "한국어" => "예", _ => "Có" };
    public string NoButton => CurrentLanguage switch { "English" => "No", "Français" => "Non", "中文" => "否", "한국어" => "아니요", _ => "Không" };

    // --- SHELL ---
    public string TabHome => CurrentLanguage switch { "English" => "Home", "Français" => "Accueil", "中文" => "主页", "한국어" => "홈", _ => "Trang chủ" };
    public string TabMap => CurrentLanguage switch { "English" => "Map", "Français" => "Carte", "中文" => "地图", "한국어" => "지도", _ => "Bản đồ" };
    public string TabQr => CurrentLanguage switch { "English" => "QR", "Français" => "QR", "中文" => "二维码", "한국어" => "QR", _ => "Mã QR" };
    public string TabSettings => CurrentLanguage switch { "English" => "Settings", "Français" => "Paramètres", "中文" => "设置", "한국어" => "설정", _ => "Cài đặt" };

    // --- MAP ---
    public string MapTitle => CurrentLanguage switch { "English" => "Tourist Map", "Français" => "Carte touristique", "中文" => "旅游地图", "한국어" => "관광 지도", _ => "Bản Đồ Du Lịch" };
    public string GpsButton => CurrentLanguage switch { "English" => "📍 Location", "Français" => "📍 Position", "中文" => "📍 位置", "한국어" => "📍 위치", _ => "📍 Vị trí" };
    public string FoodCategory => CurrentLanguage switch { "English" => "🍴 Food", "Français" => "🍴 Cuisine", "中文" => "🍴 美食", "한국어" => "🍴 음식", _ => "🍴 Thức ăn" };
    public string FunCategory => CurrentLanguage switch { "English" => "🏛️ Activities", "Français" => "🏛️ Loisirs", "中文" => "🏛️ Vui chơi", "한국어" => "🏛️ 즐길거리", _ => "🏛️ Vui chơi" };
    public string FestivalCategory => CurrentLanguage switch { "English" => "🎉 Festivals", "Français" => "🎉 Festivals", "中文" => "🎉 节庆", "한국어" => "🎉 축제", _ => "🎉 Lễ hội" };
    public string TapToViewRoute => CurrentLanguage switch { "English" => "Tap to view details & directions", "Français" => "Touchez pour voir les détails et l'itinéraire", "中文" => "点击查看详情和路线", "한국어" => "눌러서 상세 정보 및 길찾기 보기", _ => "Nhấn để xem chi tiết & chỉ đường" };
    public string ListenButton => CurrentLanguage switch { "English" => "🎧 Listen narration", "Français" => "🎧 Écouter narration", "中文" => "🎧 收听讲解", "한국어" => "🎧 안내 듣기", _ => "🎧 Nghe thuyết minh" };
    public string StopButton => CurrentLanguage switch { "English" => "⏹️ Stop", "Français" => "⏹️ Arrêter", "中文" => "⏹️ Dừng", "한국어" => "⏹️ 중지", _ => "⏹️ Dừng" };
    public string LanguageActionSheetTitle => CurrentLanguage switch { "English" => "Language", "Français" => "Langue", "中文" => "语言", "한국어" => "언어", _ => "Ngôn ngữ" };
    public string CancelButton => CurrentLanguage switch { "English" => "Cancel", "Français" => "Annuler", "中文" => "取消", "한국어" => "취소", _ => "Hủy" };
    public string SelectPoiNotice => CurrentLanguage switch { "English" => "Please select a place in the list first.", "Français" => "Veuillez d'abord sélectionner un lieu dans la liste.", "中文" => "请先在列表中选择地点。", "한국어" => "먼저 목록에서 장소를 선택하세요.", _ => "Chọn địa điểm trong danh sách trước nhé!" };
    public string PermissionRequiredTitle => CurrentLanguage switch { "English" => "Permission required", "Français" => "Autorisation requise", "中文" => "需要权限", "한국어" => "권한 필요", _ => "Thiếu quyền" };
    public string PermissionRequiredMessage => CurrentLanguage switch { "English" => "Please allow location permission to use GPS.", "Français" => "Veuillez autoriser la localisation pour utiliser le GPS.", "中文" => "请允许定位权限以使用 GPS。", "한국어" => "GPS를 사용하려면 위치 권한을 허용하세요.", _ => "Bạn cần cho phép ứng dụng dùng vị trí để lấy GPS." };
    public string GpsErrorTitle => CurrentLanguage switch { "English" => "GPS error", "Français" => "Erreur GPS", "中文" => "GPS 错误", "한국어" => "GPS 오류", _ => "Lỗi GPS" };
    public string RouteErrorTitle => CurrentLanguage switch { "English" => "Route error", "Français" => "Erreur d'itinéraire", "中文" => "路线错误", "한국어" => "경로 오류", _ => "Lỗi chỉ đường" };
    public string AudioErrorTitle => CurrentLanguage switch { "English" => "Audio error", "Français" => "Erreur audio", "中文" => "音频错误", "한국어" => "오디오 오류", _ => "Lỗi âm thanh" };
    public string GpsFetchFailedMessage => CurrentLanguage switch { "English" => "Could not get current location.", "Français" => "Impossible d'obtenir la position actuelle.", "中文" => "无法获取当前位置。", "한국어" => "현재 위치를 가져올 수 없습니다.", _ => "Không lấy được vị trí hiện tại." };
    public string RouteDrawFailedMessage => CurrentLanguage switch { "English" => "Could not draw route.", "Français" => "Impossible de tracer l'itinéraire.", "中文" => "无法绘制路线。", "한국어" => "경로를 그릴 수 없습니다.", _ => "Không vẽ được đường." };
    public string DetailPrefix => CurrentLanguage switch { "English" => "Details", "Français" => "Détails", "中文" => "详情", "한국어" => "세부 정보", _ => "Chi tiết" };
    public string ApiKeyMissingMessage => CurrentLanguage switch { "English" => "Routing API key is not configured.", "Français" => "La clé API de guidage n'est pas configurée.", "中文" => "尚未配置路线 API Key。", "한국어" => "길찾기 API 키가 설정되지 않았습니다.", _ => "Bạn chưa cấu hình API Key cho chỉ đường." };
    public string TtsServiceUnavailableMessage => CurrentLanguage switch { "English" => "No TTS service is available on this device.", "Français" => "Aucun service TTS disponible sur cet appareil.", "中文" => "设备上没有可用的 TTS 服务。", "한국어" => "이 기기에서 TTS 서비스를 사용할 수 없습니다.", _ => "Không có dịch vụ TTS trên thiết bị." };
    public string TtsPlaybackFailedMessage => CurrentLanguage switch { "English" => "Unable to play audio from both cloud and native TTS.", "Français" => "Impossible de lire l'audio via le cloud et le TTS natif.", "中文" => "云端和本机 TTS 都无法播放音频。", "한국어" => "클라우드와 기본 TTS 모두에서 오디오를 재생할 수 없습니다.", _ => "Không thể phát âm thanh cả cloud lẫn native." };
    public string ServerFetchFailedMessage => CurrentLanguage switch { "English" => "Could not fetch data from server.", "Français" => "Impossible de récupérer les données du serveur.", "中文" => "无法从服务器获取数据。", "한국어" => "서버에서 데이터를 가져올 수 없습니다.", _ => "Không lấy được dữ liệu từ server." };
    public string EmptyServerDataMessage => CurrentLanguage switch { "English" => "Server returned empty data.", "Français" => "Le serveur a renvoyé des données vides.", "中文" => "服务器返回了空数据。", "한국어" => "서버가 빈 데이터를 반환했습니다.", _ => "Dữ liệu server rỗng." };

    // --- QR PAGE ---
    public string QrPageTitle => CurrentLanguage switch { "English" => "QR", "Français" => "QR", "中文" => "二维码", "한국어" => "QR", _ => "Mã QR" };
    public string QrScanTitle => CurrentLanguage switch { "English" => "Scan QR code at the location", "Français" => "Scannez le QR du lieu", "中文" => "扫描景点二维码", "한국어" => "장소의 QR 코드를 스캔하세요", _ => "Quét mã QR tại điểm du lịch" };
    public string QrScanHint => CurrentLanguage switch { "English" => "Move the camera near the QR code to hear detailed narration.", "Français" => "Approchez la caméra du QR pour écouter la narration détaillée.", "中文" => "将相机靠近二维码以收听详细讲解。", "한국어" => "QR 코드에 카메라를 가까이 대면 상세 안내를 들을 수 있습니다.", _ => "Hãy đưa camera lại gần mã QR để nghe thuyết minh chi tiết về địa danh này." };
    public string QrNoInfoMessage => CurrentLanguage switch { "English" => "This location has no narration data yet!", "Français" => "Ce lieu ne contient pas encore d'information!", "中文" => "该地点暂时没有讲解信息！", "한국어" => "이 장소에는 아직 안내 정보가 없습니다!", _ => "Địa điểm này chưa có thông tin thuyết minh!" };
    public string GreatButton => CurrentLanguage switch { "English" => "Great", "Français" => "Super", "中文" => "太好了", "한국어" => "좋아요", _ => "Tuyệt vời" };
    public string ScanAgainButton => CurrentLanguage switch { "English" => "Scan again", "Français" => "Scanner à nouveau", "中文" => "重新扫描", "한국어" => "다시 스캔", _ => "Quét lại" };
    public string NoticeTitle => CurrentLanguage switch { "English" => "Notice", "Français" => "Notice", "中文" => "提示", "한국어" => "알림", _ => "Thông báo" };

    // --- LOCK SCREEN QR ---
    public string TicketScanTitle => CurrentLanguage switch { "English" => "Center QR code in the frame", "Français" => "Placez le QR au centre du cadre", "中文" => "将二维码对准框中心", "한국어" => "QR 코드를 프레임 중앙에 맞추세요", _ => "Đưa mã QR vào trung tâm hộp" };
    public string TicketSuccessMessage => CurrentLanguage switch { "English" => "Valid ticket confirmed!", "Français" => "Billet valide confirmé!", "中文" => "票券验证成功！", "한국어" => "유효한 티켓이 확인되었습니다!", _ => "Đã xác nhận vé hợp lệ!" };
    public string TicketSuccessButton => CurrentLanguage switch { "English" => "Enter app", "Français" => "Entrer", "中文" => "进入应用", "한국어" => "앱 시작", _ => "Vào App" };
    public string TicketInvalidMessage => CurrentLanguage switch { "English" => "Invalid ticket QR code!", "Français" => "QR billet invalide!", "中文" => "票券二维码无效！", "한국어" => "유효하지 않은 티켓 QR 코드입니다!", _ => "Mã vé không hợp lệ!" };

    public string LocalizeCategory(string rawCategory) => rawCategory switch
    {
        "Thức ăn" => CurrentLanguage switch { "English" => "Food", "Français" => "Cuisine", "中文" => "美食", "한국어" => "음식", _ => "Thức ăn" },
        "Vui chơi" => CurrentLanguage switch { "English" => "Activities", "Français" => "Loisirs", "中文" => "娱乐", "한국어" => "즐길거리", _ => "Vui chơi" },
        "Lễ hội" => CurrentLanguage switch { "English" => "Festivals", "Français" => "Festivals", "中文" => "节庆", "한국어" => "축제", _ => "Lễ hội" },
        _ => rawCategory
    };

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Helper gom logic điều hướng cấp app để các page không phải tự gán
/// Application.Current.MainPage rải rác.
///
/// Quy tắc luồng app:
///   1. Chưa quét QR (IsAppUnlocked = false) → luôn ép vào QRScannerPage.
///   2. Đã quét QR nhưng chưa chọn ngôn ngữ + giọng (pref_language_id rỗng)
///      → NavigationPage(LanguageSelectionPage) để user chọn 2 bước rồi tự vào AppShell.
///   3. Đã quét QR và đã chọn ngôn ngữ + giọng → AppShell (Home/Map/QR/Settings).
/// </summary>
public static class AppNavigation
{
    public const string PrefAppUnlocked = "IsAppUnlocked";
    public const string PrefLanguageId = "pref_language_id";
    public const string PrefLanguageCode = "pref_language_code";

    public static bool IsAppUnlocked => Preferences.Default.Get(PrefAppUnlocked, false);

    public static bool HasLanguagePreference =>
        !string.IsNullOrEmpty(Preferences.Default.Get(PrefLanguageId, string.Empty));

    /// <summary>
    /// Trả về root page phù hợp dựa trên trạng thái app hiện tại.
    /// Dùng ở Splash sau khi sync xong, và sau khi quét QR thành công.
    /// </summary>
    public static Page BuildPostSplashRoot()
    {
        if (!IsAppUnlocked)
            return new QRScannerPage();

        if (!HasLanguagePreference)
            return new NavigationPage(new LanguageSelectionPage());

        // Khôi phục ngôn ngữ UI từ code chuẩn trước khi vào AppShell để Home/Map/Settings
        // hiển thị đúng ngôn ngữ ngay lần đầu render, không phải đợi user mở Settings.
        var prefCode = Preferences.Default.Get(PrefLanguageCode, string.Empty);
        if (!string.IsNullOrWhiteSpace(prefCode))
            LocalizationManager.Instance.SetLanguageByCode(prefCode);

        return new AppShell();
    }

    /// <summary>
    /// Áp dụng root page mới cho Application.Current.MainPage.
    /// Bọc trong MainThread để an toàn khi gọi từ task nền (Splash sync, heartbeat...).
    /// </summary>
    public static void SetMainPage(Page page)
    {
        if (Application.Current is null) return;

        if (MainThread.IsMainThread)
        {
            Application.Current.MainPage = page;
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => Application.Current.MainPage = page);
        }
    }

    /// <summary>
    /// Mở lại flow chọn ngôn ngữ + giọng đọc (dùng từ Settings).
    /// Sau khi user chọn xong, VoiceSelectionPage sẽ tự gọi SetMainPage(AppShell).
    /// </summary>
    public static void OpenLanguageFlow()
        => SetMainPage(new NavigationPage(new LanguageSelectionPage()));

    /// <summary>
    /// Đi tới root phù hợp ngay bây giờ — tiện cho QRScannerPage sau khi quét xong
    /// và Splash khi đã chuẩn bị xong dữ liệu.
    /// </summary>
    public static void GoToPostSplashRoot()
        => SetMainPage(BuildPostSplashRoot());
}