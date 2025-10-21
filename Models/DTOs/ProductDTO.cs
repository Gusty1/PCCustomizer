using Newtonsoft.Json;

namespace PCCustomizer.Models.DTOs
{
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

        [JsonProperty("imgUrl")]
        public string? ImgUrl { get; set; }

        [JsonProperty("productUrl")]
        public string? ProductUrl { get; set; }

        [JsonProperty("details")]
        public List<string>? Details { get; set; }
    }
}
