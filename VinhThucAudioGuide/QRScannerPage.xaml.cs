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

        // Dùng helper chung để khỏi trùng cấu hình ZXing với QrPage; chỉ đọc QR, không cần barcode khác.
        CameraReader.Options = QrCameraOptions.Default;
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
                MarkUnlockedAndContinue(lm, verifyData);
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
                // Offline hoặc lỗi mạng — fallback kiểm tra cục bộ một mã hard-coded.
                if (result.Value == "8E8F1796A99745F4")
                {
                    MarkUnlockedAndContinue(lm, null);
                }
                else
                {
                    await DisplayAlert(lm.ErrorTitle, lm.TicketInvalidMessage, lm.ScanAgainButton);
                    CameraReader.IsDetecting = true;
                }
            }
        });
    }

    /// <summary>
    /// Đánh dấu QR hợp lệ → lưu IsAppUnlocked + sinh DeviceId + hiển thị thông báo có expiry → đi tới root tiếp theo.
    /// </summary>
    private async void MarkUnlockedAndContinue(LocalizationManager lm, Services.QrVerifyData? verifyData)
    {
        Preferences.Default.Set(AppNavigation.PrefAppUnlocked, true);
        Services.ApiService.GetOrCreateDeviceId(); // sinh và lưu UniqueDeviceId nếu chưa có

        string msg = lm.TicketSuccessMessage;
        if (verifyData?.ExpiryAt is { } expiryAt)
        {
            string expiry = expiryAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            msg += $"\n{lm.UpdateTitle}: {expiry}";
        }

        await DisplayAlert(lm.SuccessTitle, msg, lm.TicketSuccessButton);
        AppNavigation.GoToPostSplashRoot();
    }

    // Nút "✕" ở góc trên: nếu có trang trước trong NavigationPage thì pop về,
    // nếu chưa unlock thì hỏi xác nhận thoát thay vì quit không cảnh báo,
    // nếu đã unlock thì đưa user vào AppShell (đỡ lạc giữa các root).
    private async void BtnClose_Clicked(object sender, EventArgs e)
    {
        if (Navigation?.NavigationStack?.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        if (AppNavigation.IsAppUnlocked)
        {
            AppNavigation.GoToPostSplashRoot();
            return;
        }

        var lm = LocalizationManager.Instance;
        bool confirm = await DisplayAlert(lm.ExitAppTitle, lm.ExitAppMessage, lm.YesButton, lm.NoButton);
        if (confirm) Application.Current?.Quit();
    }

    // Khi user bấm back hệ thống: tái dụng logic của nút đóng ở trên.
    protected override bool OnBackButtonPressed()
    {
        BtnClose_Clicked(this, EventArgs.Empty);
        return true; // chặn back mặc định, mình tự xử lý
    }
}