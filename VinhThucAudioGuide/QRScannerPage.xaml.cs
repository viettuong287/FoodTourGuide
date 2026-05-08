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
            CameraReader.IsDetecting = false;
            var lm = LocalizationManager.Instance;

            // Gọi API xác thực mã QR vé
            var apiService = IPlatformApplication.Current?.Services.GetService<Services.ApiService>();
            Services.QrVerifyData verifyData = null;

            if (apiService != null)
            {
                verifyData = await apiService.VerifyQrCodeAsync(result.Value);
            }

            if (verifyData?.IsValid == true)
            {
                Preferences.Default.Set("IsAppUnlocked", true);

                string msg = lm.TicketSuccessMessage;
                if (verifyData.ExpiryAt.HasValue)
                {
                    string expiry = verifyData.ExpiryAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                    msg += $"\nHết hạn: {expiry}";
                }

                await DisplayAlert(lm.SuccessTitle, msg, lm.TicketSuccessButton);
                Application.Current.MainPage = new AppShell();
            }
            else if (verifyData != null && !verifyData.IsValid)
            {
                // Mã không hợp lệ theo Server
                string reason = string.IsNullOrEmpty(verifyData.Message) ? lm.TicketInvalidMessage : verifyData.Message;
                await DisplayAlert(lm.ErrorTitle, reason, lm.ScanAgainButton);
                CameraReader.IsDetecting = true;
            }
            else
            {
                // Offline hoặc lỗi mạng — fallback kiểm tra cục bộ
                if (result.Value == "8E8F1796A99745F4")
                {
                    Preferences.Default.Set("IsAppUnlocked", true);
                    await DisplayAlert(lm.SuccessTitle, lm.TicketSuccessMessage, lm.TicketSuccessButton);
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    await DisplayAlert(lm.ErrorTitle, lm.TicketInvalidMessage, lm.ScanAgainButton);
                    CameraReader.IsDetecting = true;
                }
            }
        });
    }

    private void BtnClose_Clicked(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }
}