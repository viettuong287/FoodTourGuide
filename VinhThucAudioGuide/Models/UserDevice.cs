using SQLite;
using System;

namespace VinhThucAudioGuide.Models;

public class UserDevice
{
    [PrimaryKey]
    public string DeviceId { get; set; } // Lấy chuỗi định danh máy điện thoại

    public string DeviceModel { get; set; } 
    public string SelectedLangCode { get; set; } // Khách đang chọn nghe tiếng gì
    public DateTime LastActive { get; set; } // Lần cuối mở app là khi nào
}