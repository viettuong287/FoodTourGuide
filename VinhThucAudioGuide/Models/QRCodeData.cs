using SQLite;

namespace VinhThucAudioGuide.Models;

public class QRCodeData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string CodeValue { get; set; } // Nội dung in trên QR (VD: "VINHTHUC_HAIDANG_01")
    public int LocationId { get; set; }   // Trỏ về bảng Location
}