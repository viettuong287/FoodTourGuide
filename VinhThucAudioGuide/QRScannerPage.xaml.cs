using ZXing.Net.Maui;
using Microsoft.Maui.Storage;
using System.Linq;
using System;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace VinhThucAudioGuide;

public partial class QRScannerPage : ContentPage
{
    public QRScannerPage()
    {
        InitializeComponent();
        UpdateUI();

        // CHỖ NÀY ĐÃ ĐƯỢC SỬA CHUẨN: Truyền thẳng tên mã vạch, bỏ cái ngoặc vuông mảng đi!
        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false // Quét dính 1 cái là dừng
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        UpdateUI();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
        Title = lm.TicketScanTitle;
        LblTicketScanTitle.Text = lm.TicketScanTitle;
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        Dispatcher.DispatchAsync(async () =>
        {
            // Tắt camera ngay lập tức khi bắt được mã
            CameraReader.IsDetecting = false;

            // KIỂM TRA MÃ QR
            if (result.Value == "8E8F1796A99745F4")
            {
                Preferences.Default.Set("IsAppUnlocked", true);

                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.SuccessTitle, lm.TicketSuccessMessage, lm.TicketSuccessButton);

                Application.Current.MainPage = new AppShell();
            }
            else
            {
                var lm = LocalizationManager.Instance;
                await DisplayAlert(lm.ErrorTitle, lm.TicketInvalidMessage, lm.ScanAgainButton);
                CameraReader.IsDetecting = true;
            }
        });
    }

    private void BtnClose_Clicked(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }
}