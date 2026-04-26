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

        private async void BtnGoToMap_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}