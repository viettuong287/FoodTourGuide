using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace VinhThucAudioGuide
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        // Dùng hàm này để khởi động TabBar (AppShell) là chuẩn bài không bao giờ lỗi
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}