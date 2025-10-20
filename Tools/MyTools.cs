using HtmlAgilityPack;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace PCCustomizer.Tools
{
    public static class MyTools
    {
        private readonly static string RootUrl = "https://www.coolpc.com.tw";

        /// <summary>
        /// 取得乾淨的rawText
        /// </summary>
        /// <param name="rawText">The raw text.</param>
        /// <returns></returns>
        public static string? GetClearRawText(string? rawText)
        {
            return rawText?.Trim()
                .Replace("◆", "")
                .Replace("★", "");
        }

        public static async Task<string> ExtractRedirectUrlFromLinkAsync(HttpClient httpClient, List<string> ary, int index)
        {
            try
            {
                var num = ary[index];
                // 1. 下載回傳的 JavaScript 腳本內容
                Debug.WriteLine($"正在從獲取商品網址...");
                string scriptContent = await httpClient.GetStringAsync($"https://www.coolpc.com.tw/eva-link.php?G={num}");
                Debug.WriteLine("商品網址內容下載完成。");

                // 2. 使用正規表示式來尋找 window.open('...') 中的網址
                //    - window\.open\(': 匹配 "window.open('" 這段文字
                //    - (.*?): 這是一個「捕獲組」，用來匹配並「抓住」我們想要的網址
                //    - ',': 匹配網址後面的單引號和逗號
                var regex = new Regex(@"window\.open\('(.*?)',");
                var match = regex.Match(scriptContent);

                // 3. 檢查是否成功匹配
                if (match.Success)
                {
                    // match.Groups[1].Value 就是捕獲到的網址
                    string targetUrl = match.Groups[1].Value;
                    Debug.WriteLine($"成功解析到目標網址: {targetUrl}");
                    return RootUrl + targetUrl;
                }
                else
                {
                    Debug.WriteLine("在腳本內容中找不到 'window.open(...)' 的模式。");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"訪問連結或解析腳本時發生錯誤: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> ExtractImageUrlFromPostAsync(HttpClient httpClient, List<string> ary, int index)
        {
            try
            {
                var postData = new Dictionary<string, string>
                {
                    { "G", ary[index] }
                };

                // 2. 準備要 POST 的內容
                // FormUrlEncodedContent 會自動將你的 Dictionary 轉換成 "key1=value1&key2=value2" 的格式
                using var content = new FormUrlEncodedContent(postData);

                // 3. 發送 POST 請求並獲取回應
                Debug.WriteLine($"正在向發送 POST 請求...");
                using var response = await httpClient.PostAsync("https://www.coolpc.com.tw/eva-img.php", content);

                // 確保請求成功 (狀態碼 2xx)
                response.EnsureSuccessStatusCode();

                // 4. 讀取回傳的 HTML 原始碼
                string htmlResponse = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("成功接收到 HTML 回應。");

                // 5. 使用 HtmlAgilityPack 解析 HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlResponse);

                // 6. 使用 XPath 語法來精準定位我們要的 <img> 標籤
                //    - //span[@id='ftr2']: 尋找任何位置的、id 為 'ftr2' 的 span 標籤
                //    - //img: 在該 span 標籤底下，尋找任何位置的 img 標籤
                var imgNode = doc.DocumentNode.SelectSingleNode("//span[@id='ftr2']//img");

                if (imgNode != null)
                {
                    // 7. 獲取 'src' 屬性的值
                    string imageUrl = imgNode.GetAttributeValue("src", null);
                    Debug.WriteLine($"成功找到圖片網址: {imageUrl}");

                    // 檢查網址是否有效
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        return RootUrl + imageUrl;
                    }
                }

                Debug.WriteLine("在 HTML 回應中找不到指定的圖片標籤。");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"POST 請求或解析 HTML 時發生錯誤: {ex.Message}");
                return null;
            }
        }

        public static async Task<Dictionary<string, List<string>>> ExtractAllGDataFromCoolPcAsync(HttpClient httpClient)
        {
            // 1. 建立一個空的字典來存放最終結果
            var allGData = new Dictionary<string, List<string>>();

            try
            {
                using var response = await httpClient.GetAsync("https://www.coolpc.com.tw/evaluate.php");
                response.EnsureSuccessStatusCode(); // 確保請求成功

                // 2. 將回應內容讀取為原始的 byte 陣列 (未解碼的包裹)
                byte[] rawBytes = await response.Content.ReadAsByteArrayAsync();

                // 3. 使用「Big5」編碼手動將 byte 陣列解碼為字串
                string htmlSource = Encoding.GetEncoding("Big5").GetString(rawBytes);

                Debug.WriteLine("原始碼下載並使用 Big5 解碼完成。");

                // 3. 使用正規表示式來「尋找所有」匹配 "g" 開頭陣列的模式
                //    - (g\d{1,2}): 這是一個捕獲組，用來匹配並「抓住」g1, g2...g30 這樣的鍵。
                //    - =\[(.*?)\]: 匹配等號和方括號，並用第二個捕獲組「抓住」方括號內的所有內容。
                var regex = new Regex(@"(g\d{1,2})=\[(.*?)\]");
                var matches = regex.Matches(htmlSource); // 使用 .Matches() 來獲取所有匹配項

                Debug.WriteLine($"找到 {matches.Count} 個匹配的 'g' 陣列。");

                // 4. 遍歷每一個匹配結果
                foreach (Match match in matches)
                {
                    // match.Groups[1].Value 是第一個捕獲組 (鍵，例如 "g1")
                    string key = match.Groups[1].Value;
                    // match.Groups[2].Value 是第二個捕獲組 (方括號內的字串)
                    string numberString = match.Groups[2].Value;

                    // 5. 套用你原本的清理和轉換邏輯
                    List<string> ary = numberString.Split(',')
                        .Select(s =>
                        {
                            var trimmed = s.Trim();
                            int dotIndex = trimmed.IndexOf('.');
                            return dotIndex >= 0 ? trimmed.Substring(0, dotIndex) : trimmed;
                        })
                        .Where(item => item != "0" && !string.IsNullOrEmpty(item))
                        .ToList();

                    // 6. 將處理好的結果加入字典中
                    allGData[key] = ary;
                }

                return allGData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"爬取網頁或解析資料時發生錯誤: {ex.Message}");
                // 如果發生錯誤，回傳一個空的字典
                return new Dictionary<string, List<string>>();
            }
        }
    }
}
