using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models
{
    public class MenuCategory
    {
        [Key]
        public required int Id { get; set; }

        public required string Name { get; set; }

        public DateTime? ReviseDate { get; set; }

        public required bool IsSend {  get; set; }

        public string? HtmUrl { get; set; }

        public string? PngUrl { get; set; }

        public List<Menu>? Menus { get; set; } = [];
    }
}
