using PCCustomizer.Models;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 定義查詢產品分類相關資料的服務功能。
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// (異步) 獲取所有主分類，並包含其下的子分類和產品。
        /// </summary>
        Task<List<Category>> GetCategoriesWithDetailsAsync();
    }
}