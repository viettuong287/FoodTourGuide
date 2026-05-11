using SQLite;

namespace VinhThucAudioGuide.Models;

public class Language
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ServerId { get; set; } // Liên kết với Id (Guid) trên Server

    public string LangCode { get; set; } // VD: "vi-VN", "en-US", "ja-JP"
    public string LangName { get; set; } // VD: "Tiếng Việt", "English"

    // Mã cờ ISO-2 lowercase (vn, us, fr, jp, kr, cn...) đồng bộ từ Server.
    // Mobile chuyển sang emoji 🇻🇳 / 🇺🇸 bằng regional indicator symbols.
    public string FlagCode { get; set; }
}