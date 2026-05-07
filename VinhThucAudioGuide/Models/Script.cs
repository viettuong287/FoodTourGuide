using SQLite;

namespace VinhThucAudioGuide.Models;

public class Script
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ServerId { get; set; } // Liên kết với Id (Guid) trên Server

    public int LocationId { get; set; } // Liên kết với bảng Location
    public int LanguageId { get; set; } // Liên kết với bảng Language

    public string Title { get; set; }   // Tiêu đề hiển thị (VD: "Ngọn Hải Đăng Vĩnh Thực")
    public string Content { get; set; } // Nội dung bài đọc dài thò lò
}