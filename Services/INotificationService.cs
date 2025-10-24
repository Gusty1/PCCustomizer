

namespace PCCustomizer.Services
{
    /// <summary>
    /// 通知服務的介面
    /// </summary>
    public interface INotificationService
    {
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowInfo(string message);
        void ShowWarning(string message);
    }
}
