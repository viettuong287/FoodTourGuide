using ZXing.Net.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Linq;
using System.ComponentModel;
using VinhThucAudioGuide.Services; // Gọi cái bộ não vào đây

namespace VinhThucAudioGuide;

public partial class QrPage : ContentPage
{
    // Khai báo kết nối Database
    private readonly LocalDbService _dbService;

    public QrPage()
    {
        InitializeComponent();
        UpdateUI();

        // Lấy LocalDbService từ DI để chia sẻ singleton; fallback new nếu DI không sẵn sàng (vd: test).
        _dbService = IPlatformApplication.Current?.Services.GetService<LocalDbService>() ?? new LocalDbService();

        CameraReader.Options = QrCameraOptions.Default;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        UpdateUI();
        CameraReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CameraReader.IsDetecting = false;
        LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
        {
            MainThread.BeginInvokeOnMainThread(UpdateUI);
        }
    }

    private void UpdateUI()
    {
        var lm = LocalizationManager.Instance;
        Title = lm.TabQr;
        LblScanTitle.Text = lm.QrScanTitle;
        LblScanHint.Text = lm.QrScanHint;
    }

    private void CameraReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        Dispatcher.DispatchAsync(async () =>
        {
            // Tắt camera ngay để tránh quét đúp
            CameraReader.IsDetecting = false;

            string maQRKhachQuet = result.Value;

            var lm = LocalizationManager.Instance;
            // Ưu tiên ngôn ngữ user đã chọn ở LanguageSelectionPage (pref_language_code).
            // Fallback AppLang để tương thích bản cũ chưa đi qua luồng mới.
            var prefCode = Preferences.Default.Get("pref_language_code", string.Empty);
            var langCode = !string.IsNullOrWhiteSpace(prefCode)
                ? prefCode.Split('-')[0].ToLowerInvariant()
                : Preferences.Default.Get("AppLang", "Tiếng Việt") switch
                {
                    "English" => "en",
                    "Français" => "fr",
                    "中文" => "zh",
                    "한국어" => "ko",
                    _ => "vi"
                };

            var kichBan = await _dbService.GetScriptByQRAndLanguage(maQRKhachQuet, langCode);
            if (kichBan == null && langCode != "vi")
            {
                // Không có script đúng ngôn ngữ user chọn -> rơi về tiếng Việt cho khỏi trống tay.
                // Riêng audio map đã filter ngôn ngữ ở API; chỗ này chỉ là script QR hiển thị popup.
                kichBan = await _dbService.GetScriptByQRAndLanguage(maQRKhachQuet, "vi");
            }

            if (kichBan != null)
            {
                await DisplayAlert(kichBan.Title, kichBan.Content, lm.GreatButton);
            }
            else
            {
                await DisplayAlert(lm.NoticeTitle, lm.QrNoInfoMessage, lm.ScanAgainButton);
            }

            // Xử lý xong thì bật lại camera
            CameraReader.IsDetecting = true;
        });
    }
}