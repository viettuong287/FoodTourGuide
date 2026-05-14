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
    /// Đặt ngôn ngữ UI theo language code chuẩn từ server.
    /// Nếu code không được map ở đây, các property localized sẽ rơi về tiếng Việt,
    /// nên mọi ngôn ngữ thuyết minh active trên web cần có mapping tương ứng.
    /// </summary>
    public void SetLanguageByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        var lower = code.Trim().ToLowerInvariant();
        CurrentLanguage = lower switch
        {
            "vi" or "vi-vn" or "vietnamese" or "tieng viet" or "tiếng việt" => "Tiếng Việt",
            "en" or "en-us" or "en-gb" => "English",
            "fr" or "fr-fr" => "Français",
            "zh" or "zh-cn" or "zh-tw" or "zh-hans" or "zh-hant" => "中文",
            "ko" or "ko-kr" => "한국어",
            "id" or "id-id" or "in" or "indonesian" or "bahasa indonesia" => "Bahasa Indonesia",
            "de" or "de-de" or "german" or "deutsch" => "Deutsch",
            "it" or "it-it" or "italian" or "italiano" => "Italiano",
            "ja" or "ja-jp" or "jp" or "japanese" or "japan" => "日本語",
            "ru" or "ru-ru" or "russian" => "Русский",
            "es" or "es-es" or "es-mx" or "spanish" or "espanol" or "español" => "Español",
            "th" or "th-th" or "thai" => "ไทย",
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
            "id" or "id-id" or "in" => "🇮🇩",
            "de" or "de-de" => "🇩🇪",
            "it" or "it-it" => "🇮🇹",
            "ja" or "ja-jp" or "jp" => "🇯🇵",
            "ru" or "ru-ru" => "🇷🇺",
            "es" or "es-es" or "es-mx" => "🇪🇸",
            "th" or "th-th" => "🇹🇭",
            "vi" or "vi-vn" => "🇻🇳",
            _ => "🌐"
        };
    }

    /// <summary>
    /// Chọn text theo CurrentLanguage. Các ngôn ngữ mới nếu thiếu chuỗi riêng sẽ fallback English,
    /// tuyệt đối không rơi về tiếng Việt sau khi user đã chọn ngôn ngữ khác.
    /// </summary>
    private string Pick(
        string vi,
        string en,
        string? fr = null,
        string? zh = null,
        string? ko = null,
        string? id = null,
        string? de = null,
        string? it = null,
        string? ja = null,
        string? ru = null,
        string? es = null,
        string? th = null)
        => CurrentLanguage switch
        {
            "English" => en,
            "Français" => fr ?? en,
            "中文" => zh ?? en,
            "한국어" => ko ?? en,
            "Bahasa Indonesia" => id ?? en,
            "Deutsch" => de ?? en,
            "Italiano" => it ?? en,
            "日本語" => ja ?? en,
            "Русский" => ru ?? en,
            "Español" => es ?? en,
            "ไทย" => th ?? en,
            _ => vi
        };

    // --- KỊCH BẢN / HOME ---
    public string AudioScript => Pick("Chào mừng đến với Thành phố Hồ Chí Minh. Hãy khám phá văn hóa sôi động, di tích lịch sử và những con phố nổi tiếng của thành phố năng động này.", "Welcome to Ho Chi Minh City. Explore the vibrant culture, historic landmarks, and iconic streets of this dynamic metropolis.", "Bienvenue à Hô-Chi-Minh-Ville. Explorez la culture vibrante, les sites historiques et les rues emblématiques de cette métropole dynamique.", "欢迎来到胡志明市。探索这座充满活力的大都市的文化、历史地标和标志性街道。", "호치민 시에 오신 것을 환영합니다. 이 활기찬 도시의 문화, 역사적 명소, 상징적인 거리를 탐험하세요.", "Selamat datang di Kota Ho Chi Minh. Jelajahi budaya yang hidup, tempat bersejarah, dan jalan-jalan ikonik kota ini.", "Willkommen in Ho-Chi-Minh-Stadt. Entdecken Sie die lebendige Kultur, historischen Orte und bekannten Straßen.", "Benvenuto a Ho Chi Minh City. Scopri la cultura vivace, i luoghi storici e le strade iconiche.", "ホーチミン市へようこそ。活気ある文化、歴史的名所、象徴的な通りを探索しましょう。", "Добро пожаловать в Хошимин. Откройте для себя яркую культуру, исторические места и знаменитые улицы.", "Bienvenido a Ciudad Ho Chi Minh. Explora su cultura vibrante, lugares históricos y calles icónicas.", "ยินดีต้อนรับสู่โฮจิมินห์ซิตี้ สำรวจวัฒนธรรมที่มีชีวิตชีวา สถานที่ทางประวัติศาสตร์ และถนนสำคัญของเมืองนี้");
    public string AppTitle => Pick("Audio Guide Thành phố Hồ Chí Minh", "Ho Chi Minh City Guide", "Guide Hô-Chi-Minh", "胡志明市导览", "호치민 시 가이드", "Panduan Kota Ho Chi Minh", "Ho-Chi-Minh-Stadt Guide", "Guida di Ho Chi Minh", "ホーチミン市ガイド", "Гид по Хошимину", "Guía de Ho Chi Minh", "คู่มือเมืองโฮจิมินห์");
    public string WelcomeText => Pick("Khám phá những con phố sôi động, lịch sử phong phú và các địa danh nổi tiếng của Thành phố Hồ Chí Minh.", "Explore the vibrant streets, rich history and iconic landmarks of Ho Chi Minh City.", "Découvrez les rues animées, l'histoire riche et les sites emblématiques de Hô-Chi-Minh-Ville.", "探索胡志明市充满活力的街道、丰富的历史和标志性景点。", "호치민 시의 활기찬 거리, 풍부한 역사, 상징적인 명소를 탐험하세요.", "Jelajahi jalan yang hidup, sejarah yang kaya, dan ikon Kota Ho Chi Minh.", "Entdecken Sie die lebhaften Straßen, die reiche Geschichte und die Wahrzeichen von Ho-Chi-Minh-Stadt.", "Scopri le strade vivaci, la ricca storia e i luoghi simbolo di Ho Chi Minh.", "ホーチミン市の活気ある通り、豊かな歴史、名所を探索しましょう。", "Исследуйте оживленные улицы, богатую историю и знаковые места Хошимина.", "Explora las calles vibrantes, la rica historia y los lugares emblemáticos de Ho Chi Minh.", "สำรวจถนนที่คึกคัก ประวัติศาสตร์อันยาวนาน และแลนด์มาร์กของโฮจิมินห์ซิตี้");
    public string StartButton => Pick("KHÁM PHÁ THÀNH PHỐ", "EXPLORE THE CITY", "EXPLORER LA VILLE", "探索城市", "도시 탐험하기", "JELAJAHI KOTA", "STADT ENTDECKEN", "ESPLORA LA CITTÀ", "街を探索", "ИССЛЕДОВАТЬ ГОРОД", "EXPLORAR LA CIUDAD", "สำรวจเมือง");

    // --- SETTINGS ---
    public string SettingsTitle => Pick("CÀI ĐẶT", "SETTINGS", "PARAMÈTRES", "设置", "설정", "PENGATURAN", "EINSTELLUNGEN", "IMPOSTAZIONI", "設定", "НАСТРОЙКИ", "AJUSTES", "การตั้งค่า");
    public string LanguageLabel => Pick("Ngôn ngữ ứng dụng", "App Language", "Langue de l'application", "应用语言", "앱 언어", "Bahasa Aplikasi", "App-Sprache", "Lingua app", "アプリの言語", "Язык приложения", "Idioma de la app", "ภาษาแอป");
    public string AutoPlayLabel => Pick("Tự động phát âm thanh", "Auto-play Audio", "Lecture automatique", "自动播放语音", "자동 재생", "Putar audio otomatis", "Audio automatisch abspielen", "Riproduzione automatica", "音声の自動再生", "Автовоспроизведение", "Reproducción automática", "เล่นเสียงอัตโนมัติ");
    public string VoiceSpeedLabel => Pick("Tốc độ đọc", "Voice Speed", "Vitesse de la voix", "语音语速", "음성 속도", "Kecepatan suara", "Sprechgeschwindigkeit", "Velocità voce", "音声速度", "Скорость речи", "Velocidad de voz", "ความเร็วเสียง");
    public string SaveButton => Pick("LƯU THAY ĐỔI", "SAVE CHANGES", "ENREGISTRER", "保存更改", "저장", "SIMPAN", "SPEICHERN", "SALVA", "保存", "СОХРАНИТЬ", "GUARDAR", "บันทึก");
    public string CheckUpdateButton => Pick("Cập nhật mới", "Check for updates", "Vérifier les mises à jour", "检查更新", "업데이트 확인", "Periksa pembaruan", "Nach Updates suchen", "Controlla aggiornamenti", "更新を確認", "Проверить обновления", "Buscar actualizaciones", "ตรวจสอบอัปเดต");
    public string UpdatingButton => Pick("Đang kiểm tra...", "Checking...", "Vérification...", "检查中...", "확인 중...", "Memeriksa...", "Prüfen...", "Controllo...", "確認中...", "Проверка...", "Comprobando...", "กำลังตรวจสอบ...");
    public string GroupLanguage => Pick("NGÔN NGỮ", "LANGUAGE", "LANGUE", "语言", "언어", "BAHASA", "SPRACHE", "LINGUA", "言語", "ЯЗЫК", "IDIOMA", "ภาษา");
    public string GroupAudio => Pick("ÂM THANH & THUYẾT MINH", "AUDIO & NARRATION", "AUDIO & NARRATION", "音频和讲解", "오디오 및 내레이션", "AUDIO & NARASI", "AUDIO & ERZÄHLUNG", "AUDIO E NARRAZIONE", "音声とナレーション", "АУДИО И РАССКАЗ", "AUDIO Y NARRACIÓN", "เสียงและคำบรรยาย");
    public string ChooseLanguageTitle => Pick("Chọn ngôn ngữ", "Choose language", "Choisir la langue", "选择语言", "언어 선택", "Pilih bahasa", "Sprache wählen", "Scegli lingua", "言語を選択", "Выберите язык", "Elegir idioma", "เลือกภาษา");
    public string SaveSuccessMessage => Pick("Lưu cài đặt thành công!", "Settings saved successfully!", "Paramètres enregistrés avec succès!", "设置已保存！", "설정이 저장되었습니다!", "Pengaturan berhasil disimpan!", "Einstellungen gespeichert!", "Impostazioni salvate!", "設定を保存しました！", "Настройки сохранены!", "¡Ajustes guardados!", "บันทึกการตั้งค่าแล้ว!");
    public string UpdateSuccessMessage(int added) => Pick($"Đã đồng bộ xong. Thêm {added} địa điểm mới.", $"Sync completed. Added {added} new places.", $"Synchronisation terminée. {added} nouveaux lieux ajoutés.", $"同步完成。已新增 {added} 个地点。", $"동기화 완료. 새 장소 {added}개가 추가되었습니다.", $"Sinkron selesai. Menambahkan {added} tempat baru.", $"Synchronisierung abgeschlossen. {added} neue Orte hinzugefügt.", $"Sincronizzazione completata. Aggiunti {added} nuovi luoghi.", $"同期完了。新しい場所を{added}件追加しました。", $"Синхронизация завершена. Добавлено мест: {added}.", $"Sincronización completada. Se agregaron {added} lugares.", $"ซิงค์เสร็จแล้ว เพิ่มสถานที่ใหม่ {added} แห่ง");
    public string ErrorTitle => Pick("Lỗi", "Error", "Erreur", "错误", "오류", "Kesalahan", "Fehler", "Errore", "エラー", "Ошибка", "Error", "ข้อผิดพลาด");
    public string SuccessTitle => Pick("Thành công", "Success", "Succès", "成功", "성공", "Berhasil", "Erfolg", "Successo", "成功", "Успешно", "Éxito", "สำเร็จ");
    public string UpdateTitle => Pick("Cập nhật", "Update", "Mise à jour", "更新", "업데이트", "Pembaruan", "Update", "Aggiornamento", "更新", "Обновление", "Actualización", "อัปเดต");
    public string OkButton => Pick("OK", "OK", "OK", "确定", "확인", "OK", "OK", "OK", "OK", "OK", "OK", "ตกลง");

    // --- ONBOARDING (chọn ngôn ngữ + giọng đọc) ---
    public string OnboardingLanguageTitle => Pick("Chọn ngôn ngữ thuyết minh", "Choose narration language", "Choisir la langue de narration", "选择讲解语言", "내레이션 언어 선택", "Pilih bahasa narasi", "Sprache der Führung wählen", "Scegli lingua narrazione", "ナレーション言語を選択", "Выберите язык аудиогида", "Elige idioma de narración", "เลือกภาษาคำบรรยาย");
    public string OnboardingLanguageSubtitle => Pick("Hãy chọn ngôn ngữ bạn muốn nghe. Bước sau bạn sẽ chọn giọng đọc.", "Pick the language you want to hear. Next you will choose a voice.", "Choisissez la langue à écouter. Vous choisirez ensuite une voix.", "选择你想要听的语言。下一步将选择朗读声音。", "원하는 언어를 선택하세요. 다음 단계에서 음성을 선택합니다.", "Pilih bahasa yang ingin Anda dengar. Berikutnya pilih suara.", "Wählen Sie die gewünschte Sprache. Danach wählen Sie eine Stimme.", "Scegli la lingua da ascoltare. Poi sceglierai una voce.", "聞きたい言語を選択してください。次に音声を選択します。", "Выберите язык, который хотите слушать. Затем выберите голос.", "Elige el idioma que quieres escuchar. Luego elegirás una voz.", "เลือกภาษาที่ต้องการฟัง จากนั้นเลือกเสียง");
    public string OnboardingVoiceTitle => Pick("Chọn giọng đọc", "Choose a voice", "Choisir une voix", "选择朗读声音", "음성 선택", "Pilih suara", "Stimme wählen", "Scegli voce", "音声を選択", "Выберите голос", "Elegir voz", "เลือกเสียง");
    public string OnboardingVoiceDefaultLabel => Pick("Mặc định", "Default", "Par défaut", "默认", "기본", "Default", "Standard", "Predefinito", "既定", "По умолчанию", "Predeterminado", "ค่าเริ่มต้น");
    public string OnboardingVoiceSubtitle => Pick("Giọng đọc", "Voice", "Voix", "声音", "음성", "Suara", "Stimme", "Voce", "音声", "Голос", "Voz", "เสียง");
    public string OnboardingSkipVoiceButton => Pick("Bỏ qua, dùng giọng mặc định", "Skip, use default voice", "Passer, utiliser la voix par défaut", "跳过，使用默认声音", "건너뛰기, 기본 음성 사용", "Lewati, gunakan suara default", "Überspringen, Standardstimme nutzen", "Salta, usa voce predefinita", "スキップして既定の音声を使用", "Пропустить, использовать голос по умолчанию", "Omitir, usar voz predeterminada", "ข้าม ใช้เสียงเริ่มต้น");
    public string OnboardingRetryButton => Pick("Tải lại", "Retry", "Réessayer", "重试", "다시 시도", "Coba lagi", "Erneut versuchen", "Riprova", "再試行", "Повторить", "Reintentar", "ลองใหม่");
    public string OnboardingNoLanguages => Pick("Không tải được danh sách ngôn ngữ.", "Could not load language list.", "Impossible de charger la liste des langues.", "无法加载语言列表。", "언어 목록을 불러올 수 없습니다.", "Tidak dapat memuat daftar bahasa.", "Sprachliste konnte nicht geladen werden.", "Impossibile caricare la lista lingue.", "言語リストを読み込めません。", "Не удалось загрузить список языков.", "No se pudo cargar la lista de idiomas.", "ไม่สามารถโหลดรายการภาษาได้");
    public string OnboardingCheckNetwork => Pick("Kiểm tra kết nối mạng và thử lại.", "Check your network connection and try again.", "Vérifiez votre connexion réseau et réessayez.", "请检查网络连接后重试。", "네트워크 연결을 확인하고 다시 시도하세요.", "Periksa koneksi jaringan lalu coba lagi.", "Prüfen Sie die Netzwerkverbindung und versuchen Sie es erneut.", "Controlla la connessione e riprova.", "ネットワーク接続を確認して再試行してください。", "Проверьте подключение и повторите попытку.", "Revisa la conexión e inténtalo de nuevo.", "ตรวจสอบเครือข่ายแล้วลองอีกครั้ง");
    public string OnboardingNoVoices => Pick("Chưa có giọng đọc cho ngôn ngữ này.", "No voices available for this language.", "Aucune voix disponible pour cette langue.", "该语言暂无可用声音。", "이 언어에 사용할 수 있는 음성이 없습니다.", "Belum ada suara untuk bahasa ini.", "Für diese Sprache sind keine Stimmen verfügbar.", "Nessuna voce disponibile per questa lingua.", "この言語の音声はまだありません。", "Для этого языка нет голосов.", "No hay voces disponibles para este idioma.", "ยังไม่มีเสียงสำหรับภาษานี้");
    public string OnboardingSkipHint => Pick("Bấm Bỏ qua để dùng giọng mặc định.", "Tap Skip to use the default voice.", "Touchez Passer pour utiliser la voix par défaut.", "点击“跳过”使用默认声音。", "기본 음성을 사용하려면 건너뛰기를 누르세요.", "Ketuk Lewati untuk memakai suara default.", "Tippen Sie auf Überspringen für die Standardstimme.", "Tocca Salta per usare la voce predefinita.", "既定の音声を使うにはスキップをタップしてください。", "Нажмите Пропустить, чтобы использовать голос по умолчанию.", "Toca Omitir para usar la voz predeterminada.", "แตะข้ามเพื่อใช้เสียงเริ่มต้น");
    public string OnboardingSaving => Pick("Đang lưu lựa chọn...", "Saving your choice...", "Enregistrement de votre choix...", "正在保存选择...", "선택 저장 중...", "Menyimpan pilihan...", "Auswahl wird gespeichert...", "Salvataggio scelta...", "選択を保存中...", "Сохранение выбора...", "Guardando elección...", "กำลังบันทึกตัวเลือก...");

    // --- MAP / COMMON ---
    public string MapLoadingPois => Pick("Đang nạp địa điểm...", "Loading places...", "Chargement des lieux...", "正在加载地点...", "장소 불러오는 중...", "Memuat tempat...", "Orte werden geladen...", "Caricamento luoghi...", "場所を読み込み中...", "Загрузка мест...", "Cargando lugares...", "กำลังโหลดสถานที่...");
    public string MapEmpty => Pick("Chưa có địa điểm nào.", "No places yet.", "Aucun lieu pour le moment.", "暂无地点。", "아직 등록된 장소가 없습니다.", "Belum ada tempat.", "Noch keine Orte.", "Nessun luogo disponibile.", "場所はまだありません。", "Пока нет мест.", "Aún no hay lugares.", "ยังไม่มีสถานที่");
    public string PoiMissingAudioTitle => Pick("Chưa có thuyết minh", "No narration", "Pas de narration", "暂无讲解", "안내 없음", "Belum ada narasi", "Keine Erzählung", "Nessuna narrazione", "ナレーションなし", "Нет аудиогида", "Sin narración", "ยังไม่มีคำบรรยาย");
    public string PoiMissingAudioMessage => Pick("Địa điểm này chưa có audio thuyết minh. Vui lòng tạo Audio URL trên trang quản trị web rồi thử lại.", "This place has no narration audio yet. Please add an audio URL on the admin web and try again.", "Ce lieu n'a pas encore de narration audio. Veuillez ajouter une URL audio dans l'admin web puis réessayer.", "该地点暂无讲解音频。请在管理后台添加音频 URL 后重试。", "이 장소에는 안내 오디오가 없습니다. 관리 페이지에서 오디오 URL을 추가한 뒤 다시 시도하세요.", "Tempat ini belum memiliki audio narasi. Tambahkan Audio URL di web admin lalu coba lagi.", "Für diesen Ort gibt es noch kein Audio. Bitte fügen Sie im Admin-Web eine Audio-URL hinzu.", "Questo luogo non ha ancora audio. Aggiungi un URL audio nel pannello admin e riprova.", "この場所にはまだナレーション音声がありません。管理画面でAudio URLを追加してください。", "Для этого места нет аудио. Добавьте Audio URL в админ-панели и повторите.", "Este lugar aún no tiene audio. Agrega una URL de audio en el admin web e inténtalo de nuevo.", "สถานที่นี้ยังไม่มีเสียงคำบรรยาย กรุณาเพิ่ม Audio URL ในหน้าแอดมินแล้วลองอีกครั้ง");
    public string BackButton => Pick("Quay lại", "Back", "Retour", "返回", "뒤로", "Kembali", "Zurück", "Indietro", "戻る", "Назад", "Atrás", "ย้อนกลับ");
    public string CloseButton => Pick("Đóng", "Close", "Fermer", "关闭", "닫기", "Tutup", "Schließen", "Chiudi", "閉じる", "Закрыть", "Cerrar", "ปิด");
    public string ExitAppTitle => Pick("Thoát ứng dụng", "Exit app", "Quitter l'app", "退出应用", "앱 종료", "Keluar aplikasi", "App beenden", "Esci dall'app", "アプリを終了", "Выйти из приложения", "Salir de la app", "ออกจากแอป");
    public string ExitAppMessage => Pick("Bạn có chắc muốn thoát?", "Are you sure you want to exit?", "Voulez-vous vraiment quitter?", "确定要退出吗？", "정말 종료하시겠습니까?", "Yakin ingin keluar?", "Möchten Sie wirklich beenden?", "Vuoi davvero uscire?", "終了しますか？", "Вы уверены, что хотите выйти?", "¿Seguro que quieres salir?", "ต้องการออกจริงหรือไม่?");
    public string YesButton => Pick("Có", "Yes", "Oui", "是", "예", "Ya", "Ja", "Sì", "はい", "Да", "Sí", "ใช่");
    public string NoButton => Pick("Không", "No", "Non", "否", "아니요", "Tidak", "Nein", "No", "いいえ", "Нет", "No", "ไม่");

    // --- SHELL / MAP ---
    public string TabHome => Pick("Trang chủ", "Home", "Accueil", "主页", "홈", "Beranda", "Start", "Home", "ホーム", "Главная", "Inicio", "หน้าหลัก");
    public string TabMap => Pick("Bản đồ", "Map", "Carte", "地图", "지도", "Peta", "Karte", "Mappa", "地図", "Карта", "Mapa", "แผนที่");
    public string TabQr => Pick("Mã QR", "QR", "QR", "二维码", "QR", "QR", "QR", "QR", "QR", "QR", "QR", "QR");
    public string TabSettings => Pick("Cài đặt", "Settings", "Paramètres", "设置", "설정", "Pengaturan", "Einstellungen", "Impostazioni", "設定", "Настройки", "Ajustes", "การตั้งค่า");
    public string MapTitle => Pick("Bản Đồ Du Lịch", "Tourist Map", "Carte touristique", "旅游地图", "관광 지도", "Peta Wisata", "Touristenkarte", "Mappa turistica", "観光マップ", "Туристическая карта", "Mapa turístico", "แผนที่ท่องเที่ยว");
    public string GpsButton => Pick("📍 Vị trí", "📍 Location", "📍 Position", "📍 位置", "📍 위치", "📍 Lokasi", "📍 Standort", "📍 Posizione", "📍 現在地", "📍 Место", "📍 Ubicación", "📍 ตำแหน่ง");
    public string FoodCategory => Pick("🍴 Thức ăn", "🍴 Food", "🍴 Cuisine", "🍴 美食", "🍴 음식", "🍴 Makanan", "🍴 Essen", "🍴 Cibo", "🍴 食事", "🍴 Еда", "🍴 Comida", "🍴 อาหาร");
    public string FunCategory => Pick("🏛️ Vui chơi", "🏛️ Activities", "🏛️ Loisirs", "🏛️ 娱乐", "🏛️ 즐길거리", "🏛️ Aktivitas", "🏛️ Aktivitäten", "🏛️ Attività", "🏛️ アクティビティ", "🏛️ Развлечения", "🏛️ Actividades", "🏛️ กิจกรรม");
    public string FestivalCategory => Pick("🎉 Lễ hội", "🎉 Festivals", "🎉 Festivals", "🎉 节庆", "🎉 축제", "🎉 Festival", "🎉 Festivals", "🎉 Festival", "🎉 祭り", "🎉 Фестивали", "🎉 Festivales", "🎉 เทศกาล");
    public string TapToViewRoute => Pick("Nhấn để xem chi tiết & chỉ đường", "Tap to view details & directions", "Touchez pour voir les détails et l'itinéraire", "点击查看详情和路线", "눌러서 상세 정보 및 길찾기 보기", "Ketuk untuk detail & rute", "Tippen für Details & Route", "Tocca per dettagli e indicazioni", "タップして詳細と経路を表示", "Нажмите для деталей и маршрута", "Toca para ver detalles y ruta", "แตะเพื่อดูรายละเอียดและเส้นทาง");
    public string ListenButton => Pick("🎧 Nghe thuyết minh", "🎧 Listen narration", "🎧 Écouter narration", "🎧 收听讲解", "🎧 안내 듣기", "🎧 Dengarkan narasi", "🎧 Audioführung hören", "🎧 Ascolta narrazione", "🎧 ナレーションを聞く", "🎧 Слушать аудиогид", "🎧 Escuchar narración", "🎧 ฟังคำบรรยาย");
    /// <summary>Chuỗi định dạng — tham số {0} là số lượt chờ (server + hàng chờ POI trên máy).</summary>
    public string QueueWaitRemainingFormat => Pick("Lượt đợi còn: {0}", "Waiting turns: {0}", "File d'attente : {0}", "等待顺序: {0}", "대기 순번: {0}", "Menunggu giliran: {0}", "Warteschlange: {0}", "Turni in attesa: {0}", "待ち: {0}", "Очередь: {0}", "Turnos de espera: {0}", "คิวที่เหลือ: {0}");
    public string StopButton => Pick("⏹️ Dừng", "⏹️ Stop", "⏹️ Arrêter", "⏹️ 停止", "⏹️ 중지", "⏹️ Berhenti", "⏹️ Stoppen", "⏹️ Stop", "⏹️ 停止", "⏹️ Стоп", "⏹️ Detener", "⏹️ หยุด");
    public string LanguageActionSheetTitle => Pick("Ngôn ngữ", "Language", "Langue", "语言", "언어", "Bahasa", "Sprache", "Lingua", "言語", "Язык", "Idioma", "ภาษา");
    public string CancelButton => Pick("Hủy", "Cancel", "Annuler", "取消", "취소", "Batal", "Abbrechen", "Annulla", "キャンセル", "Отмена", "Cancelar", "ยกเลิก");
    public string SelectPoiNotice => Pick("Chọn địa điểm trong danh sách trước nhé!", "Please select a place in the list first.", "Veuillez d'abord sélectionner un lieu dans la liste.", "请先在列表中选择地点。", "먼저 목록에서 장소를 선택하세요.", "Pilih tempat dari daftar terlebih dahulu.", "Bitte zuerst einen Ort in der Liste wählen.", "Seleziona prima un luogo dalla lista.", "先にリストから場所を選択してください。", "Сначала выберите место из списка.", "Primero selecciona un lugar de la lista.", "กรุณาเลือกสถานที่จากรายการก่อน");
    public string PermissionRequiredTitle => Pick("Thiếu quyền", "Permission required", "Autorisation requise", "需要权限", "권한 필요", "Izin diperlukan", "Berechtigung erforderlich", "Permesso richiesto", "権限が必要", "Требуется разрешение", "Permiso requerido", "ต้องการสิทธิ์");
    public string PermissionRequiredMessage => Pick("Bạn cần cho phép ứng dụng dùng vị trí để lấy GPS.", "Please allow location permission to use GPS.", "Veuillez autoriser la localisation pour utiliser le GPS.", "请允许定位权限以使用 GPS。", "GPS를 사용하려면 위치 권한을 허용하세요.", "Izinkan akses lokasi untuk memakai GPS.", "Bitte erlauben Sie Standortzugriff für GPS.", "Consenti l'accesso alla posizione per usare il GPS.", "GPSを使うには位置情報の許可が必要です。", "Разрешите доступ к геолокации для GPS.", "Permite acceso a ubicación para usar GPS.", "โปรดอนุญาตตำแหน่งเพื่อใช้ GPS");
    public string GpsErrorTitle => Pick("Lỗi GPS", "GPS error", "Erreur GPS", "GPS 错误", "GPS 오류", "Kesalahan GPS", "GPS-Fehler", "Errore GPS", "GPSエラー", "Ошибка GPS", "Error GPS", "ข้อผิดพลาด GPS");
    public string RouteErrorTitle => Pick("Lỗi chỉ đường", "Route error", "Erreur d'itinéraire", "路线错误", "경로 오류", "Kesalahan rute", "Routenfehler", "Errore percorso", "経路エラー", "Ошибка маршрута", "Error de ruta", "ข้อผิดพลาดเส้นทาง");
    public string AudioErrorTitle => Pick("Lỗi âm thanh", "Audio error", "Erreur audio", "音频错误", "오디오 오류", "Kesalahan audio", "Audiofehler", "Errore audio", "音声エラー", "Ошибка аудио", "Error de audio", "ข้อผิดพลาดเสียง");
    public string GpsFetchFailedMessage => Pick("Không lấy được vị trí hiện tại.", "Could not get current location.", "Impossible d'obtenir la position actuelle.", "无法获取当前位置。", "현재 위치를 가져올 수 없습니다.", "Tidak dapat mengambil lokasi saat ini.", "Aktueller Standort konnte nicht ermittelt werden.", "Impossibile ottenere la posizione attuale.", "現在地を取得できません。", "Не удалось получить текущее местоположение.", "No se pudo obtener la ubicación actual.", "ไม่สามารถรับตำแหน่งปัจจุบันได้");
    public string RouteDrawFailedMessage => Pick("Không vẽ được đường.", "Could not draw route.", "Impossible de tracer l'itinéraire.", "无法绘制路线。", "경로를 그릴 수 없습니다.", "Tidak dapat menggambar rute.", "Route konnte nicht gezeichnet werden.", "Impossibile tracciare il percorso.", "経路を描画できません。", "Не удалось построить маршрут.", "No se pudo dibujar la ruta.", "ไม่สามารถวาดเส้นทางได้");
    public string DetailPrefix => Pick("Chi tiết", "Details", "Détails", "详情", "세부 정보", "Detail", "Details", "Dettagli", "詳細", "Подробности", "Detalles", "รายละเอียด");
    public string ApiKeyMissingMessage => Pick("Bạn chưa cấu hình API Key cho chỉ đường.", "Routing API key is not configured.", "La clé API de guidage n'est pas configurée.", "尚未配置路线 API Key。", "길찾기 API 키가 설정되지 않았습니다.", "API key rute belum dikonfigurasi.", "Routing-API-Key ist nicht konfiguriert.", "API key percorso non configurata.", "経路APIキーが未設定です。", "API-ключ маршрута не настроен.", "La API key de rutas no está configurada.", "ยังไม่ได้ตั้งค่า API Key สำหรับเส้นทาง");
    public string TtsServiceUnavailableMessage => Pick("Không có dịch vụ TTS trên thiết bị.", "No TTS service is available on this device.", "Aucun service TTS disponible sur cet appareil.", "设备上没有可用的 TTS 服务。", "이 기기에서 TTS 서비스를 사용할 수 없습니다.", "Layanan TTS tidak tersedia di perangkat ini.", "Kein TTS-Dienst auf diesem Gerät verfügbar.", "Nessun servizio TTS disponibile su questo dispositivo.", "この端末ではTTSサービスを利用できません。", "На устройстве нет службы TTS.", "No hay servicio TTS disponible en este dispositivo.", "ไม่มีบริการ TTS บนอุปกรณ์นี้");
    public string TtsPlaybackFailedMessage => Pick("Không thể phát âm thanh cả cloud lẫn native.", "Unable to play audio from both cloud and native TTS.", "Impossible de lire l'audio via le cloud et le TTS natif.", "云端和本机 TTS 都无法播放音频。", "클라우드와 기본 TTS 모두에서 오디오를 재생할 수 없습니다.", "Tidak dapat memutar audio dari cloud maupun TTS perangkat.", "Audio kann weder über Cloud noch natives TTS abgespielt werden.", "Impossibile riprodurre audio cloud o TTS nativo.", "クラウド音声も端末TTSも再生できません。", "Не удалось воспроизвести аудио из облака и TTS устройства.", "No se pudo reproducir audio de nube ni TTS nativo.", "ไม่สามารถเล่นเสียงจากคลาวด์หรือ TTS เครื่องได้");
    public string ServerFetchFailedMessage => Pick("Không lấy được dữ liệu từ server.", "Could not fetch data from server.", "Impossible de récupérer les données du serveur.", "无法从服务器获取数据。", "서버에서 데이터를 가져올 수 없습니다.", "Tidak dapat mengambil data dari server.", "Daten konnten nicht vom Server geladen werden.", "Impossibile recuperare dati dal server.", "サーバーからデータを取得できません。", "Не удалось получить данные с сервера.", "No se pudieron obtener datos del servidor.", "ไม่สามารถดึงข้อมูลจากเซิร์ฟเวอร์ได้");
    public string EmptyServerDataMessage => Pick("Dữ liệu server rỗng.", "Server returned empty data.", "Le serveur a renvoyé des données vides.", "服务器返回了空数据。", "서버가 빈 데이터를 반환했습니다.", "Server mengembalikan data kosong.", "Server hat leere Daten zurückgegeben.", "Il server ha restituito dati vuoti.", "サーバーが空のデータを返しました。", "Сервер вернул пустые данные.", "El servidor devolvió datos vacíos.", "เซิร์ฟเวอร์ส่งข้อมูลว่างกลับมา");

    // --- QR ---
    public string QrPageTitle => TabQr;
    public string QrScanTitle => Pick("Quét mã QR tại điểm du lịch", "Scan QR code at the location", "Scannez le QR du lieu", "扫描景点二维码", "장소의 QR 코드를 스캔하세요", "Pindai QR di lokasi", "QR-Code am Ort scannen", "Scansiona il QR sul posto", "現地のQRコードをスキャン", "Сканируйте QR-код на месте", "Escanea el QR del lugar", "สแกน QR ณ สถานที่");
    public string QrScanHint => Pick("Hãy đưa camera lại gần mã QR để nghe thuyết minh chi tiết về địa danh này.", "Move the camera near the QR code to hear detailed narration.", "Approchez la caméra du QR pour écouter la narration détaillée.", "将相机靠近二维码以收听详细讲解。", "QR 코드에 카메라를 가까이 대면 상세 안내를 들을 수 있습니다.", "Arahkan kamera ke QR untuk mendengar narasi detail.", "Halten Sie die Kamera an den QR-Code, um die Führung zu hören.", "Avvicina la fotocamera al QR per ascoltare la narrazione.", "QRコードにカメラを近づけると詳細な案内を聞けます。", "Поднесите камеру к QR-коду, чтобы услышать подробный рассказ.", "Acerca la cámara al QR para escuchar la narración.", "นำกล้องเข้าใกล้ QR เพื่อฟังคำบรรยายโดยละเอียด");
    public string QrNoInfoMessage => Pick("Địa điểm này chưa có thông tin thuyết minh!", "This location has no narration data yet!", "Ce lieu ne contient pas encore d'information!", "该地点暂时没有讲解信息！", "이 장소에는 아직 안내 정보가 없습니다!", "Lokasi ini belum memiliki data narasi!", "Für diesen Ort gibt es noch keine Informationen!", "Questo luogo non ha ancora dati di narrazione!", "この場所にはまだ案内データがありません！", "Для этого места пока нет данных аудиогида!", "Este lugar aún no tiene datos de narración!", "สถานที่นี้ยังไม่มีข้อมูลคำบรรยาย!");
    public string GreatButton => Pick("Tuyệt vời", "Great", "Super", "太好了", "좋아요", "Bagus", "Super", "Ottimo", "了解", "Отлично", "Genial", "เยี่ยม");
    public string ScanAgainButton => Pick("Quét lại", "Scan again", "Scanner à nouveau", "重新扫描", "다시 스캔", "Pindai lagi", "Erneut scannen", "Scansiona di nuovo", "再スキャン", "Сканировать снова", "Escanear de nuevo", "สแกนอีกครั้ง");
    public string NoticeTitle => Pick("Thông báo", "Notice", "Notice", "提示", "알림", "Pemberitahuan", "Hinweis", "Avviso", "通知", "Уведомление", "Aviso", "แจ้งเตือน");
    public string TicketScanTitle => Pick("Đưa mã QR vào trung tâm hộp", "Center QR code in the frame", "Placez le QR au centre du cadre", "将二维码对准框中心", "QR 코드를 프레임 중앙에 맞추세요", "Posisikan QR di tengah bingkai", "QR-Code in die Mitte des Rahmens halten", "Centra il QR nel riquadro", "QRコードを枠の中央に合わせてください", "Поместите QR-код в центр рамки", "Centra el QR en el marco", "วาง QR ไว้กลางกรอบ");
    public string TicketSuccessMessage => Pick("Đã xác nhận vé hợp lệ!", "Valid ticket confirmed!", "Billet valide confirmé!", "票券验证成功！", "유효한 티켓이 확인되었습니다!", "Tiket valid dikonfirmasi!", "Gültiges Ticket bestätigt!", "Biglietto valido confermato!", "有効なチケットを確認しました！", "Действительный билет подтвержден!", "¡Ticket válido confirmado!", "ยืนยันตั๋วถูกต้องแล้ว!");
    public string TicketSuccessButton => Pick("Vào App", "Enter app", "Entrer", "进入应用", "앱 시작", "Masuk app", "App öffnen", "Entra nell'app", "アプリに入る", "Войти в приложение", "Entrar a la app", "เข้าแอป");
    public string TicketInvalidMessage => Pick("Mã vé không hợp lệ!", "Invalid ticket QR code!", "QR billet invalide!", "票券二维码无效！", "유효하지 않은 티켓 QR 코드입니다!", "Kode QR tiket tidak valid!", "Ungültiger Ticket-QR-Code!", "QR del biglietto non valido!", "チケットQRコードが無効です！", "Недействительный QR-код билета!", "¡QR de ticket no válido!", "QR ตั๋วไม่ถูกต้อง!");

    public string LocalizeCategory(string rawCategory) => rawCategory switch
    {
        "Thức ăn" => FoodCategory.Replace("🍴 ", string.Empty),
        "Vui chơi" => FunCategory.Replace("🏛️ ", string.Empty),
        "Lễ hội" => FestivalCategory.Replace("🎉 ", string.Empty),
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