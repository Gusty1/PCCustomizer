using Newtonsoft.Json;

namespace PCCustomizer.Models.DTOs 
{
    /// <summary>
    /// 獲取json檔案中的子目錄資料
    /// </summary>
    public class SubcategoryDTO
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("products")]
        public List<ProductDTO>? Products { get; set; }
    }
}