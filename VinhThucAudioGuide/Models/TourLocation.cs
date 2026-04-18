using SQLite;

namespace VinhThucAudioGuide.Models;

public class TourLocation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string LocationName { get; set; }
    public string Category { get; set; } // Thêm cái này để phân loại Food/Entertainment...
    public string ImageUrl { get; set; } // Thêm trường ảnh ở đây
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}