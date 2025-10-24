using Microsoft.AspNetCore.Components;
using PCCustomizer.Services;
using System.Diagnostics;

namespace PCCustomizer.Components
{
    /// <summary>
    /// razor的共用方法
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
    public abstract class BaseComponent : ComponentBase
    {
        // 注入通知服務
        [Inject]
        protected INotificationService NotificationService { get; set; } = default!;

        // 開啟連結
        protected async Task OpenExternalLink(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                if (await Launcher.CanOpenAsync(url))
                {
                    bool success = await Launcher.OpenAsync(url);
                    if (!success)
                    {
                        NotificationService.ShowError($"無法開啟連結");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"開啟連結時發生錯誤", ex.Message);
                NotificationService.ShowError($"開啟連結時發生錯誤");
            }
        }
    }
}