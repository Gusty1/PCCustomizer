

namespace PCCustomizer.Models.DTOs
{
    /// <summary>
    /// 首頁我的商品資料
    /// </summary>
    public class MyProductDTO
    {
        public required string Index { get; set; }

        public required string SubcategoryName { get; set; }

        public string? Group { get; set; }

        public int? Price { get; set; }

        public List<string>? Markers { get; set; } = [];

        public string? RawText { get; set; }

        public string? FullText { get; set; }

        public string? ImgUrl { get; set; }

        public string? ProductUrl { get; set; }

        public List<string>? Details { get; set; } = [];

        //記錄我目前的商品數量
        public int Qty { get; set; }
    }
}
