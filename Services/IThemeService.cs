using MudBlazor;
using System.ComponentModel;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 主題管理服務的介面，定義了需要被實作的功能。
    /// </summary>
    public interface IThemeService : INotifyPropertyChanged
    {
        /// <summary>
        /// 當主題狀態改變時觸發的事件。
        /// </summary>
        event Action? OnChange;

        /// <summary>
        /// MudBlazor 的當前主題物件。
        /// </summary>
        MudTheme CurrentTheme { get; }

        /// <summary>
        /// 當前是否為暗色模式。
        /// </summary>
        bool IsDarkMode { get; }

        /// <summary>
        /// 切換亮暗模式。
        /// </summary>
        void ToggleDarkMode(bool isDarkMode);

        /// <summary>
        /// (異步) 從使用者偏好設定中載入主題。
        /// </summary>
        Task LoadThemeAsync();
    }
}