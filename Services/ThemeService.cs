using CommunityToolkit.Mvvm.ComponentModel;
using MudBlazor;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 主題管理服務的具體實作。
    /// </summary>
    public class ThemeService(IPreferences preferences) : ObservableObject, IThemeService
    {
        private const string ThemeKey = "AppTheme_IsDark"; // 用於儲存設定的 Key
        private bool _isDarkMode = false;

        public event Action? OnChange; // 主題變更時觸發的事件

        public MudTheme CurrentTheme { get; set; } = new MudTheme(); // MudBlazor 主題物件

        public bool IsDarkMode
        {
            get => _isDarkMode;
            private set
            {
                // 當值改變時，更新屬性並觸發 OnChange 事件通知 UI
                if (SetProperty(ref _isDarkMode, value))
                {
                    OnChange?.Invoke();
                }
            }
        }

        // 從裝置讀取已儲存的主題設定
        public Task LoadThemeAsync()
        {
            IsDarkMode = preferences.Get(ThemeKey, false);
            return Task.CompletedTask;
        }

        // 切換模式並儲存設定到裝置
        public void ToggleDarkMode(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
            preferences.Set(ThemeKey, IsDarkMode);
        }
    }
}