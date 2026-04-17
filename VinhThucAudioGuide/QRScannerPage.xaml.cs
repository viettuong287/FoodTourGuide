using ZXing.Net.Maui;
using Microsoft.Maui.Storage;
using System.Linq;
using System;
using Microsoft.Maui.Controls;

namespace VinhThucAudioGuide;

public partial class QRScannerPage : ContentPage
{
    public QRScannerPage()
    {
        InitializeComponent();

        // CHỖ NÀY ĐÃ ĐƯỢC SỬA CHUẨN: Truyền thẳng tên mã vạch, bỏ cái ngoặc vuông mảng đi!
        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false // Quét dính 1 cái là dừng
        };
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        Dispatcher.DispatchAsync(async () =>
        {
            // Tắt camera ngay lập tức khi bắt được mã
            CameraReader.IsDetecting = false;

            // KIỂM TRA MÃ VIP
            if (result.Value == "FOODTOUR_VIP_2026")
            {
                Preferences.Default.Set("IsAppUnlocked", true);

                await DisplayAlert("Thành công", "Đã xác nhận vé hợp lệ!", "Vào App");

                Application.Current.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("Lỗi", "Mã vé không hợp lệ!", "Quét lại");
                CameraReader.IsDetecting = true;
            }
        });
    }

    private void BtnClose_Clicked(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }
}