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
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(string.Empty); // refresh all dependent localized properties
        }
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

    // --- HOME ---
    public string AppTitle => CurrentLanguage switch { "English" => "Vinh Thuc Guide", "Français" => "Guide Vinh Thuc", "中文" => "永实岛导览", "한국어" => "빈득 안내", _ => "Vĩnh Thực Audio Guide" };
    public string WelcomeText => CurrentLanguage switch { "English" => "Discover island beauty through automated stories.", "Français" => "Découvrez l'île à travers des récits automatiques.", "中文" => "通过自动讲解探索岛屿之美。", "한국어" => "자동 내레이션으로 섬의 아름다움을 발견하세요.", _ => "Khám phá vẻ đẹp biển đảo qua từng câu chuyện kể tự động." };
    public string StartButton => CurrentLanguage switch { "English" => "START JOURNEY", "Français" => "COMMENCER", "中文" => "开始旅程", "한국어" => "여행 시작", _ => "BẮT ĐẦU HÀNH TRÌNH" };

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