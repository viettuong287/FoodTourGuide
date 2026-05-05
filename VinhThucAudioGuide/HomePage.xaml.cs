using Microsoft.Maui.Controls;
using System.ComponentModel;
using System;

namespace VinhThucAudioGuide
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            UpdateUI();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            SetNativeBackground();
        }

        /// <summary>
        /// Đặt background trực tiếp vào Android native view - bypass hoàn toàn MAUI Shell/ContentPage rendering.
        /// Đây là cách chắc chắn nhất để hiển thị ảnh background khi trang nằm trong Shell.
        /// </summary>
        private void SetNativeBackground()
        {
#if ANDROID
            try
            {
                if (Handler?.PlatformView is Android.Views.View androidView)
                {
                    var context = Android.App.Application.Context;

                    // Thử load drawable "hinhnenapp" từ Android resource
                    var resId = context.Resources!.GetIdentifier(
                        "hinhnenapp", "drawable", context.PackageName);

                    if (resId != 0)
                    {
                        androidView.SetBackgroundResource(resId);
                        return;
                    }

                    // Fallback: đặt màu đen nếu không tìm thấy drawable
                    androidView.SetBackgroundColor(Android.Graphics.Color.Black);
                }
            }
            catch
            {
                // Ignore - không crash app nếu không set được background
            }
#endif
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            UpdateUI();
            // Gọi lại để đảm bảo background được set sau khi trang hoàn toàn hiển thị
            SetNativeBackground();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
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

        private async void BtnGoToMap_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}