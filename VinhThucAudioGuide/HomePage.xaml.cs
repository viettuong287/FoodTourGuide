using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System;

namespace VinhThucAudioGuide
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            UpdateUI();
            // Load ảnh ngay trong constructor trước khi trang hiện
            LoadBackgroundImage();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            UpdateUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        /// <summary>
        /// Load ảnh từ EmbeddedResource - nhúng thẳng trong DLL, bypass hoàn toàn mọi pipeline
        /// </summary>
        private void LoadBackgroundImage()
        {
            try
            {
                var assembly = typeof(HomePage).Assembly;
                var resourceName = "VinhThucAudioGuide.Resources.Images.bgtrangchu.jpg";
                var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    // Tìm kiếm fallback nếu tên resource khác
                    var all = assembly.GetManifestResourceNames();
                    var match = all.FirstOrDefault(n => n.Contains("bgtrangchu"));
                    if (match != null)
                        stream = assembly.GetManifestResourceStream(match);
                }

                if (stream != null)
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    stream.Dispose();
                    var bytes = ms.ToArray();
                    ImgBackground.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                else
                {
                    // Last resort fallback
                    ImgBackground.Source = "hinhnenapp.jpg";
                }
            }
            catch
            {
                ImgBackground.Source = "hinhnenapp.jpg";
            }
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
            {
                MainThread.BeginInvokeOnMainThread(UpdateUI);
            }
        }

        private void UpdateUI()
        {
            var lm = LocalizationManager.Instance;
            Title = lm.TabHome;
            LblTitle.Text = lm.AppTitle;
            LblDesc.Text = lm.WelcomeText;
            BtnStart.Text = lm.StartButton;
        }

        // Nút chuyển sang tab Bản đồ (nằm trong Shell nên dùng GoToAsync)
        private async void BtnGoToMap_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}