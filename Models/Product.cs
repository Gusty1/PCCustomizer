using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public required string SubcategoryName { get; set; }

        public required string Index { get; set; }

        public string? Group { get; set; }

        public string? Brand { get; set; }

        public List<string>? Specs { get; set; } = [];

        public int? Price { get; set; }

        public List<string>? Markers { get; set; } = [];

        public string? RawText { get; set; }
    }
}
