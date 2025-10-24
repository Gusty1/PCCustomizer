using Newtonsoft.Json;

namespace PCCustomizer.Models.DTOs
{
    /// <summary>
    /// 獲取json檔案中的主目錄資料
    /// </summary>
    public class CategoryDTO
    {
        [JsonProperty("category_id")]
        public required string CategoryId { get; set; }

        [JsonProperty("category_name")]
        public required string CategoryName { get; set; }

        [JsonProperty("summary")]
        public required string Summary { get; set; }

        [JsonProperty("subcategories")]
        public List<SubcategoryDTO>? Subcategories { get; set; }
    }
}