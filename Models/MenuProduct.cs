using System.ComponentModel.DataAnnotations;

namespace PCCustomizer.Models
{
    /// <summary>
    /// table MenuProduct 設定
    /// </summary>
    public class MenuProduct
    {
        [Key]
        public int Id { get; set; }

        public int MenuCategoryId { get; set; }

        public int CategoryId { get; set; }

        public string CateroyName { get; set; }

        public string SubcategoryName { get; set; }

        public string ProductName { get; set; }

        public string ProdctFullText { get; set; }

        public int ProductPrice { get; set; }

        public int Seq { get; set; }

        public int Qty { get; set; }
    }
}
