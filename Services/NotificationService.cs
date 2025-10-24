using MudBlazor;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 通知服務的實作
    /// </summary>
    /// <seealso cref="PCCustomizer.Services.INotificationService" />
    public class NotificationService : INotificationService
    {
        private readonly ISnackbar _snackbar;

        // 透過依賴注入取得 MudBlazor 的 ISnackbar 實例
        public NotificationService(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        /// <summary>
        /// 顯示成功的訊息
        /// </summary>
        /// <param name="message">要顯示的訊息</param>
        public void ShowSuccess(string message)
        {
            _snackbar.Add(message, Severity.Success);
        }

        /// <summary>
        /// 顯示錯誤的訊息
        /// </summary>
        /// <param name="message">要顯示的訊息</param>
        public void ShowError(string message)
        {
            _snackbar.Add(message, Severity.Error, config => { config.CloseAfterNavigation = true; });
        }

        /// <summary>
        /// 顯示一般的提示訊息
        /// </summary>
        /// <param name="message">要顯示的訊息</param>
        public void ShowInfo(string message)
        {
            _snackbar.Add(message, Severity.Info);
        }

        /// <summary>
        /// 顯示警告訊息
        /// </summary>
        /// <param name="message">要顯示的訊息</param>
        public void ShowWarning(string message)
        {
            _snackbar.Add(message, Severity.Warning);
        }
    }
}
