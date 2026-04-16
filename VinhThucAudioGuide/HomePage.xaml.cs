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
            // Lệnh này bảo hệ thống Shell chuyển hướng sang Tab có Route là "MainPage"
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}