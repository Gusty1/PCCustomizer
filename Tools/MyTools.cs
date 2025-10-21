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
    }
}
