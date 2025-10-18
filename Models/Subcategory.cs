using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models 
{
    public class Subcategory
    {
        [Key]
        public int Id { get; set; }

        public required int CategoryId { get; set; }

        public required string SubcategoryName { get; set; }

        public List<Product>? Products { get; set; } = [];
    }
}