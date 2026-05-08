using SQLite;

namespace VinhThucAudioGuide.Models;

public class TourLocation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ServerId { get; set; } // Liên kết với Id (Guid) trên Server
    public string LocationName { get; set; }
    public string Address { get; set; }
    public string ImageUrl { get; set; } // Thêm trường ảnh ở đây
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}