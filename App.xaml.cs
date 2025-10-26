using PCCustomizer.Services;

namespace PCCustomizer
{
    public partial class App : Application
    {
        public App(IUpdateCheckService updateService)
        {
            InitializeComponent();

            MainPage = new MainPage();

            _ = updateService.CheckAndNotifyUpdatesAsync();
        }
    }
}
