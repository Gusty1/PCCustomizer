using System.Diagnostics;
using System.Text.Json;

namespace PCCustomizer.Services 
{
    /// <summary>
    /// 檢查github release是否有更新服務
    /// </summary>
    /// <seealso cref="PCCustomizer.Services.IUpdateCheckService" />
    public class UpdateCheckService : IUpdateCheckService
    {
        private const string GitHubOwner = "gusty1";
        private const string GitHubRepo = "PCCustomizer";
        private const string ApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
        private const string DownloadUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/latest";

        private readonly HttpClient _httpClient;

        public UpdateCheckService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 實作：從 GitHub 取得最新的版本號字串。
        /// </summary>
        public async Task<string> GetLatestVersionAsync()
        {
            try
            {
                // (如果您使用選擇二方案，請保留這兩行)
                // _httpClient.DefaultRequestHeaders.Remove("User-Agent");
                // _httpClient.DefaultRequestHeaders.Add("User-Agent", "PCCustomizer-Update-Check");

                var response = await _httpClient.GetAsync(ApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error getting latest version: {response.StatusCode}");
                    return "檢查失敗";
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                string latestVersionStr = doc.RootElement.GetProperty("tag_name").GetString() ?? "0.0.0";

                // 清理版本號 (例如 "v1.0.1" -> "1.0.1")
                latestVersionStr = latestVersionStr.TrimStart('v');
                return latestVersionStr;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetLatestVersionAsync: {ex.Message}");
                return "檢查失敗";
            }
        }

        /// <summary>
        /// 實作：檢查並彈出通知。
        /// </summary>
        public async Task CheckAndNotifyUpdatesAsync()
        {
            try
            {
                string currentVersionStr = AppInfo.Current.VersionString;
                var currentVersion = new Version(currentVersionStr);

                // 呼叫我們剛剛建立的新方法
                string latestVersionStr = await GetLatestVersionAsync();
                if (latestVersionStr == "檢查失敗")
                {
                    return; // 網路錯誤或 API 失敗，靜默處理
                }

                var latestVersion = new Version(latestVersionStr);

                // 比較版本並彈出對話框
                if (latestVersion > currentVersion)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        bool goToDownload = await Application.Current.MainPage.DisplayAlert(
                            "發現新版本",
                            $"PCCustomizer {latestVersionStr} 已經發布了！\n\n" +
                            $"您目前使用的是 {currentVersionStr}。\n" +
                            "是否前往 GitHub 下載頁面？",
                            "前往下載",
                            "稍後再說");

                        if (goToDownload)
                        {
                            await Browser.Default.OpenAsync(DownloadUrl, BrowserLaunchMode.SystemPreferred);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕捉所有例外，確保 App 啟動時不會閃退
                Debug.WriteLine($"Exception in CheckAndNotifyUpdatesAsync: {ex.Message}");
            }
        }
    }
}