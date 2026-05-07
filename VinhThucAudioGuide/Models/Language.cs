using SQLite;

namespace VinhThucAudioGuide.Models;

public class Language
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ServerId { get; set; } // Liên kết với Id (Guid) trên Server

    public string LangCode { get; set; } // VD: "vi", "en", "ja"
    public string LangName { get; set; } // VD: "Tiếng Việt", "English"
}