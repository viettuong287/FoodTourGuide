using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using Microsoft.Maui.Storage;
using System.Linq;

namespace VinhThucAudioGuide;

public partial class QRScannerPage : ContentPage
{
    public QRScannerPage()
    {
        InitializeComponent();

        // Cấu hình Camera đơn giản, an toàn không báo lỗi
        CameraReader.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = false
        }; 

    private void CameraReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault();
        if (result != null)
        {
            // Bắt buộc chạy trên luồng giao diện (UI Thread)
            Dispatcher.DispatchAsync(async () =>
            {
                // 1. Tạm dừng camera ngay lập tức để không quét bồi thêm
                CameraReader.IsDetecting = false;

                // ==========================================
                // 2. KHAI BÁO MÃ QR CỨNG (HARDCODED)
                // ==========================================
                string maBiMatCuaSep = "8E8F1796A99745F4";

                // 3. Kiểm tra xem mã khách quét có đúng mã VIP không
                if (result.Value == maBiMatCuaSep)
                {
                    // Đánh dấu vào sổ tay là vé xịn, đã qua cửa
                    Preferences.Default.Set("IsAppUnlocked", true);

                    await DisplayAlert("Kích hoạt thành công", "Chào mừng bạn đến với Tour Vĩnh Thực!", "Bắt đầu");

                    // Chuyển thẳng vào giao diện chính của app (AppShell)
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    // Nếu quét mã tào lao (ví dụ mã lon bò húc, mã momo người khác...)
                    await DisplayAlert("Vé không hợp lệ", "Mã QR này không đúng. Vui lòng quét mã trên vé của bạn!", "Quét lại");

                    // Bật camera lên cho quét lại
                    CameraReader.IsDetecting = true;
                }
            });
        }
    }
}