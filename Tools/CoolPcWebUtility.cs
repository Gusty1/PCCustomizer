using PCCustomizer.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions; 
using System.Web; 

namespace PCCustomizer.Tools
{
    /// <summary>
    /// 提供爬取和操作原價屋估價單相關功能的工具類。
    /// </summary>
    public static class CoolPcWebUtility
    {
        private static readonly HttpClient httpClient;
        private static readonly Encoding big5Encoding;

        /// <summary>
        /// 靜態建構函數：註冊 Big5 編碼並初始化 HttpClient。
        /// </summary>
        static CoolPcWebUtility()
        {
            // --- 初始化 Big5 編碼 ---
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                big5Encoding = Encoding.GetEncoding("big5");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[嚴重] 無法載入 'big5' 編碼: {e.Message}");
                big5Encoding = Encoding.Default; // Fallback
            }

            // --- 初始化 HttpClient ---
            var handler = new HttpClientHandler()
            {
                UseCookies = false,
            };

            httpClient = new HttpClient(handler);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");
        }

        /// <summary>
        /// 獲取原價屋網站的 PHPSESSID
        /// </summary>
        public static async Task<string?> GetCoolPcSessionIdAsync()
        {
            var url = "https://www.coolpc.com.tw/evaluate.php";
            try
            {
                Debug.WriteLine($"正在向 {url} 發送 GET 請求 (含預設標頭)...");
                var response = await httpClient.GetAsync(url);
                Debug.WriteLine($"收到回應狀態碼: {response.StatusCode}");

                response.EnsureSuccessStatusCode();

                // 嘗試從回應標頭中讀取 "Set-Cookie"
                if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                {
                    Debug.WriteLine("在 Response Headers 中找到 Set-Cookie 標頭:");
                    foreach (var header in cookieHeaders)
                    {
                        Debug.WriteLine($"- {header}");
                        if (header.StartsWith("PHPSESSID="))
                        {
                            string sessionIdPart = header.Split(';').First();
                            string sessionIdValue = sessionIdPart.Split('=')[1];
                            Debug.WriteLine($"成功提取 PHPSESSID: {sessionIdValue}");
                            return sessionIdValue;
                        }
                    }
                    Debug.WriteLine("Set-Cookie 標頭中未找到 PHPSESSID。");
                }
                else
                {
                    Debug.WriteLine("在 Response Headers 中未找到 Set-Cookie 標頭。");
                }

                return null;
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"網路請求錯誤: {e.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"處理回應時發生未預期錯誤: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 發送估價單請求，返回原始 HTML
        /// </summary>
        public static async Task<string?> SendEstimateAsync(string sessionId, Dictionary<string, string> payload)
        {
            if (big5Encoding == null || big5Encoding.EncodingName == Encoding.Default.EncodingName)
            {
                Debug.WriteLine("Big5 編碼不可用，已終止請求。");
                return null;
            }

            var url = "https://www.coolpc.com.tw/eval-save.php";

            try
            {
                // 處理資料：替換特殊字元
                var processedPayload = new Dictionary<string, string>();
                foreach (var (key, value) in payload)
                {
                    processedPayload[key] = (value ?? string.Empty).Replace('\u21a9', '\n');
                }

                // Big5 編碼
                var encodedPairs = new List<string>();
                foreach (var (key, value) in processedPayload)
                {
                    string encodedKey = HttpUtility.UrlEncode(key, big5Encoding);
                    string encodedValue = HttpUtility.UrlEncode(value, big5Encoding);
                    encodedPairs.Add($"{encodedKey}={encodedValue}");
                }
                string big5EncodedString = string.Join("&", encodedPairs);

                // 建立請求
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);

                // 必要標頭
                requestMessage.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
                requestMessage.Headers.Add("Accept-Language", "zh-TW,zh;q=0.9");
                requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                requestMessage.Headers.Referrer = new Uri("https://www.coolpc.com.tw/eval-save.php");
                requestMessage.Headers.Add("Origin", "https://www.coolpc.com.tw");
                requestMessage.Headers.Add("Cookie", $"PHPSESSID={sessionId}");

                // Body（Big5 編碼）
                requestMessage.Content = new StringContent(big5EncodedString, big5Encoding);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                requestMessage.Content.Headers.ContentType.CharSet = "big5";

                // 發送請求
                Debug.WriteLine($"正在向 {url} 發送 POST 請求 (含 Cookie)...");
                var response = await httpClient.SendAsync(requestMessage);
                Debug.WriteLine($"收到回應狀態碼: {response.StatusCode}");

                response.EnsureSuccessStatusCode();

                // 解碼回應
                string decodedContent;
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "";
                Encoding responseEncoding = big5Encoding; // 預設 Big5

                if (contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string charset = contentType.Split("charset=")[1].Split(';')[0].Trim();
                        responseEncoding = Encoding.GetEncoding(charset);
                        if (!responseEncoding.EncodingName.Equals(big5Encoding.EncodingName, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"警告：伺服器回應非 Big5 ({responseEncoding.EncodingName})，但仍嘗試使用 Big5 解碼。");
                            responseEncoding = big5Encoding;
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"無法解析 Content-Type 中的 charset，將使用預設 Big5。 Content-Type: {contentType}");
                    }
                }
                else
                {
                    Debug.WriteLine($"回應未指定 charset，將使用預設 Big5。 Content-Type: {contentType}");
                }

                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                decodedContent = responseEncoding.GetString(responseBytes);

                return decodedContent;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"發送估價單時發生錯誤: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析 eval-save.php 的 POST 回應 HTML，從 JavaScript 中提取檔名
        /// </summary>
        public static (string? HtmFilename, string? PngFilename)? ParseEstimateResult(string? htmlContent) // [!! 修改 !!] 返回值改為檔名
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                Debug.WriteLine("ParseEstimateResult 收到空 HTML，無法解析。");
                return null;
            }

            string? htmFilename = null;
            string? pngFilename = null;

            try
            {
                // [!! 修正 Regex !!] 匹配 JavaScript 中的檔名賦值
                // 模式：f.fname.value=...+ '/tmp/' + '檔名.htm'; (忽略空白和引號)
                var fnameMatch = Regex.Match(htmlContent, @"f\.fname\.value\s*=\s*.*?\/tmp\/'\s*\+\s*'([^']+)'", RegexOptions.IgnoreCase);
                if (fnameMatch.Success && fnameMatch.Groups.Count > 1)
                {
                    htmFilename = fnameMatch.Groups[1].Value; // 取得檔名 (例如 1761399614139508.htm)
                    Debug.WriteLine($"成功解析 HTM 檔名: {htmFilename}");
                }
                else
                {
                    Debug.WriteLine("在 HTML 的 JavaScript 中未找到 fname 的賦值模式。");
                }

                // 模式：f.iname.value=...+ '/tmp/' + '檔名.png'; (忽略空白和引號)
                var inameMatch = Regex.Match(htmlContent, @"f\.iname\.value\s*=\s*.*?\/tmp\/'\s*\+\s*'([^']+)'", RegexOptions.IgnoreCase);
                if (inameMatch.Success && inameMatch.Groups.Count > 1)
                {
                    pngFilename = inameMatch.Groups[1].Value; // 取得檔名 (例如 1761399614139508.png)
                    Debug.WriteLine($"成功解析 PNG 檔名: {pngFilename}");
                }
                else
                {
                    Debug.WriteLine("在 HTML 的 JavaScript 中未找到 iname 的賦值模式。");
                }

                // 只有當 htmFilename 或 pngFilename 至少有一個被成功解析時才回傳 Tuple
                if (htmFilename != null || pngFilename != null)
                {
                    return (htmFilename, pngFilename);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析估價單 POST 回應 HTML 時發生錯誤: {ex.Message}");
            }

            Debug.WriteLine("ParseEstimateResult 解析完成，未找到任何檔名。");
            return null; // 解析失敗或未找到
        }

        /// <summary>
        /// 發送估價單請求並直接解析回傳的 HTML 以獲取 HTM 和 PNG 檔名
        /// </summary>
        public static async Task<(string? HtmFilename, string? PngFilename)?> SendAndParseEstimateAsync(string sessionId, Dictionary<string, string> payload) // [!! 修改 !!] 返回值改為檔名
        {
            string? htmlResponse = await SendEstimateAsync(sessionId, payload);

            if (string.IsNullOrEmpty(htmlResponse))
            {
                Debug.WriteLine("SendEstimateAsync 未返回有效的 HTML 內容。");
                return null;
            }

            var parsedResult = ParseEstimateResult(htmlResponse);
            if (parsedResult == null)
            {
                Debug.WriteLine("SendAndParseEstimateAsync: ParseEstimateResult 返回 null。");
            }
            else
            {
                Debug.WriteLine($"SendAndParseEstimateAsync: 解析結果 HtmFilename={parsedResult.Value.HtmFilename ?? "null"}, PngFilename={parsedResult.Value.PngFilename ?? "null"}");
            }
            return parsedResult;
        }

        /// <summary>
        /// 不知道原價屋怎麼寫的亂七八糟的html
        /// </summary>
        /// <param name="products">The products.</param>
        /// <returns></returns>
        public static string BuildPayLoad(List<MenuProduct> products)
        {
            var sb = new StringBuilder();

            // 開始表格
            sb.Append("<table cellspacing=1 bgcolor=black>");
            sb.Append("<tr bgcolor=#cceeee><td>品 名<td>產 品 名 稱<td>備 註<td>數量<td>小 計</tr>");

            // 每個產品列
            foreach (var product in products)
            {
                sb.Append("<tr bgcolor=#eeeecc>");
                sb.Append($"<td>{product.CateroyName}<td>{product.ProdctFullText}<td><div bis_skin_checked=\"1\">{product.SubcategoryName}</div><td>{product.Qty}<td><div bis_skin_checked=\"1\">{product.ProductPrice * product.Qty}</div>");
            }

            // 總價列
            int total = products.Sum(x => x.ProductPrice * x.Qty);
            sb.Append($"<tr bgcolor=gold><td colspan=5 align=right>TDP耗電X瓦 　含稅 現金價：{total}");

            // 結束表格
            sb.Append("</table>");

            return sb.ToString();
        }
    }
}

