using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PCCustomizer.Components
{
    // 將 internal 改為 public，確保其他元件可以繼承
    public abstract class BaseComponent : ComponentBase
    {
        // 在這裡注入所有常用的服務

        //mudblazor的snackbar
        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        // =========================================================
        // 新增共用方法：OpenExternalLink
        // 設為 protected 讓所有繼承元件都能使用
        // =========================================================
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
                        Snackbar?.Add($"無法開啟連結：{url}", Severity.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar?.Add($"開啟連結時發生錯誤：{ex.Message}", Severity.Error);
            }
        }
    }
}