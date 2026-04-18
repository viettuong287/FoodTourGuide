using SQLite;
using System;

namespace VinhThucAudioGuide.Models;

public class UserDevice
{
    [PrimaryKey]
    public string DeviceId { get; set; } // Lấy chuỗi định danh máy điện thoại

    public string DeviceModel { get; set; } // VD: "iPhone 13", "Xiaomi Redmi"
    public string SelectedLangCode { get; set; } // Khách đang chọn nghe tiếng gì
    public DateTime LastActive { get; set; } // Lần cuối mở app là khi nào
}