using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VinhThucAudioGuide.Models;
using VinhThucAudioGuide.Services; // Gọi bộ não SQLite vào đây

namespace VinhThucAudioGuide.ViewModels;

public partial class MapViewModel : ObservableObject
{
    // 1. CHUYỂN TỪ FIREBASE SANG SQLITE OFFLINE
    private readonly LocalDbService _dbService;

    // 2. CHUYỂN KHUÔN CŨ (DiemThamQuan) SANG KHUÔN MỚI (TourLocation)
    [ObservableProperty]
    private ObservableCollection<TourLocation> danhSachDiem;

    [ObservableProperty]
    private bool isBusy;

    public MapViewModel()
    {
        // Khởi tạo bộ não DB nội bộ
        _dbService = new LocalDbService();
        DanhSachDiem = new ObservableCollection<TourLocation>();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        IsBusy = true;

        try
        {
            // 3. GỌI HÀM LẤY TOÀN BỘ ĐỊA ĐIỂM TỪ SQLITE 
            var data = await _dbService.GetAllTourLocations();

            DanhSachDiem.Clear();
            foreach (var item in data)
            {
                DanhSachDiem.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}