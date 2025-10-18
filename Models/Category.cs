using PCCustomizer.Models.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models
{
    public class Category
    {
        [Key]
        public required int CategoryId { get; set; } 

        public required string CategoryName { get; set; }

        public required string Summary { get; set; } 

        public List<Subcategory>? Subcategories { get; set; } = [];
    }
}
