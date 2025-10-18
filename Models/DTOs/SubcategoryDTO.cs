using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models.DTOs 
{
    public class SubcategoryDTO
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("products")]
        public List<ProductDTO>? Products { get; set; }
    }
}