using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models.DTOs // 請確認這是你專案的正確名稱
{
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