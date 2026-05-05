namespace VinhThucAudioGuide
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            LocalizationManager.Instance.PropertyChanged += (_, _) => UpdateTabs();
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            var lm = LocalizationManager.Instance;
            TabHome.Title = lm.TabHome;
            TabMap.Title = lm.TabMap;
            TabQr.Title = lm.TabQr;
            TabSettings.Title = lm.TabSettings;
        }
    }
}
