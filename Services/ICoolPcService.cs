using PCCustomizer.Models;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 定義與原價屋網站互動（取得 Session、發送估價單、解析回應）的服務介面。
    /// </summary>
    public interface ICoolPcService
    {
        /// <summary>
        /// 向原價屋首頁發送 GET 請求以取得 PHPSESSID。
        /// </summary>
        Task<string?> GetSessionIdAsync();

        /// <summary>
        /// 根據商品列表建立估價單 HTML Payload 字串。
        /// </summary>
        string BuildPayload(List<MenuProduct> products);

        /// <summary>
        /// 發送估價單並解析回傳 HTML，回傳 HTM 與 PNG 的檔名。
        /// </summary>
        Task<(string? HtmFilename, string? PngFilename)?> SendAndParseEstimateAsync(
            string sessionId, Dictionary<string, string> payload);
    }
}
