using ZXing.Net.Maui;
using Microsoft.Maui.Controls;
using System.Linq;
using VinhThucAudioGuide.Services; // Gọi cái bộ não vào đây

namespace VinhThucAudioGuide;

public partial class QrPage : ContentPage
{
    // Khai báo kết nối Database
    private readonly LocalDbService _dbService;

    public QrPage()
    {
        InitializeComponent();

        // Khởi tạo Database ngay khi mở trang
        _dbService = new LocalDbService();

        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CameraReader.IsDetecting = false;
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

            // Đem cái mã vừa quét đi hỏi Database (Tạm thời tìm bản Tiếng Việt - "vi")
            var kichBan = await _dbService.GetScriptByQRAndLanguage(maQRKhachQuet, "vi");

            if (kichBan != null)
            {
                // NẾU TÌM THẤY TRONG DB: Đẩy tiêu đề và nội dung lên màn hình!
                await DisplayAlert(kichBan.Title, kichBan.Content, "Tuyệt vời");
            }
            else
            {
                // NẾU MÃ BẬY BẠ HOẶC CHƯA CÓ TRONG DB
                await DisplayAlert("Thông báo", "Địa điểm này chưa có thông tin thuyết minh!", "Quét lại");
            }

            // Xử lý xong thì bật lại camera
            CameraReader.IsDetecting = true;
        });
    }
}