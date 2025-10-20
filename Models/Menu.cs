using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models
{
    public class Menu
    {
        [Key]
        public required int Id { get; set; }

        public Category? Category { get; set; }

        public string? RawText { get; set; }

        public int? Price { get; set; }

    }
}
