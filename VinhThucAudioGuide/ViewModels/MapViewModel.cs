using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VinhThucAudioGuide.Models;
using VinhThucAudioGuide.Services;

namespace VinhThucAudioGuide.ViewModels;

// Chú ý bắt buộc phải có chữ 'partial' và kế thừa ObservableObject
public partial class MapViewModel : ObservableObject
{
    private readonly FirebaseService _dbService;

    // Danh sách các điểm tham quan để hiện lên giao diện
    [ObservableProperty]
    private ObservableCollection<DiemThamQuan> danhSachDiem;

    // Biến cờ hiệu: Đang tải dữ liệu (True/False) để hiện vòng xoay xoay
    [ObservableProperty]
    private bool isBusy;

    public MapViewModel()
    {
        _dbService = new FirebaseService();
        DanhSachDiem = new ObservableCollection<DiemThamQuan>();
    }

    // Cái [RelayCommand] này siêu xịn, nó tự biến hàm này thành một cái Command để giao diện gọi được
    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return; // Nếu đang tải rồi thì không bấm được nữa

        IsBusy = true; // Bật vòng xoay

        try
        {
            var data = await _dbService.LayDuLieuAsync();
            DanhSachDiem.Clear();
            foreach (var item in data)
            {
                DanhSachDiem.Add(item);
            }
        }
        finally
        {
            IsBusy = false; // Tắt vòng xoay
        }
    }
}