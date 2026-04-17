using ZXing.Net.Maui;
using Microsoft.Maui.Controls;
using System.Linq;

namespace VinhThucAudioGuide;

public partial class QrPage : ContentPage
{
    public QrPage()
    {
        InitializeComponent();

        // Cấu hình bộ quét siêu tốc (Chỉ đọc QR Code)
        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    // ==========================================
    // TỐI ƯU PIN: Chỉ bật camera khi người dùng mở Tab này
    // ==========================================
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CameraReader.IsDetecting = false; // Chuyển tab khác là tắt camera cho mát máy
    }

    // ==========================================
    // XỬ LÝ KHI QUÉT TRÚNG MÃ QR
    // ==========================================
    private void CameraReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        Dispatcher.DispatchAsync(async () =>
        {
            // Tạm dừng camera ngay lập tức
            CameraReader.IsDetecting = false;

            // Lấy nội dung của mã QR
            string noiDungMaQR = result.Value;

            // Hiện thông báo (Sau này sếp có thể thay chỗ này bằng code gọi hàm đọc âm thanh)
            await DisplayAlert("Đã nhận diện", $"Nội dung mã: {noiDungMaQR}\n\nĐang tải thuyết minh...", "Nghe");

            // Xử lý xong thì bật camera lại để khách quét điểm tiếp theo
            CameraReader.IsDetecting = true;
        });
    }
}