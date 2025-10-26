namespace PCCustomizer.Services
{
    public interface IUpdateCheckService
    {
        /// <summary>
        /// 從 GitHub 取得最新的版本號字串 (例如 "1.0.1")。
        /// </summary>
        /// <returns>版本號字串，或 "檢查失敗"</returns>
        Task<string> GetLatestVersionAsync();

        /// <summary>
        /// 檢查是否有新版本，如果有，則彈出通知對話框。
        /// (此方法供 App 啟動時使用)
        /// </summary>
        Task CheckAndNotifyUpdatesAsync();
    }
}