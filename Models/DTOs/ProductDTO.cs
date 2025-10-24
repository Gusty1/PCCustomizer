using Newtonsoft.Json;

namespace PCCustomizer.Models.DTOs
{
    /// <summary>
    /// 獲取json檔案中的商品資料
    /// </summary>
    public class ProductDTO
    {
        [JsonProperty("index")]
        public required string Index { get; set; }

        [JsonProperty("group")]
        public string? Group { get; set; }

        [JsonProperty("price")]
        public int? Price { get; set; }

        [JsonProperty("discount_amount")]
        public int? Discount { get; set; }

        [JsonProperty("markers")]
        public List<string>? Markers { get; set; }

        [JsonProperty("raw_text")]
        public string? RawText { get; set; }

        [JsonProperty("full_text")]
        public string? FullText { get; set; }

        [JsonProperty("img_url")]
        public string? ImgUrl { get; set; }

        [JsonProperty("product_url")]
        public string? ProductUrl { get; set; }

        [JsonProperty("details")]
        public List<string>? Details { get; set; }
    }
}
