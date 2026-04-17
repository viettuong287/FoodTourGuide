using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Khai báo thêm cái này lỡ bro cần đồng bộ ngôn ngữ ở Trang Chủ
using System;

namespace VinhThucAudioGuide
{
    public partial class HomePage : ContentPage 
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void BtnGoToMap_Clicked(object sender, EventArgs e)
        {
            // Lệnh nhảy sang trang Bản đồ
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}