using PCCustomizer.Models;
using PCCustomizer.Models.DTOs;
using System.ComponentModel;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 定義查詢產品分類相關資料的服務功能。
    /// </summary>
    public interface IMenuService : INotifyPropertyChanged
    {
        bool IsLoading { get; }
        List<MenuCategory> MenuCategories { get;}

        event Action OnStateChanged;

        Task AddMenuCategory();
        
        Task GetMenuCategoriesAsync();

        Task AddMenuProduct(MenuCategory menuCategory, MyCategoryDTO myCategoryDTO,
            MyProductDTO myProductDTO, int qty);

        Task<List<Dictionary<string, List<MenuProduct>>>> GetDictMyMenu(MenuCategory menuCategory);

        Task<List<MyMenuCategoryDTO>> GetMyMenuCtegoryDTOs();

        Task DeleteMenuCategory(int id);

        Task UpdateMenuCategoryAsync(MyMenuCategoryDTO myMenuCategoryDTO,List<MenuProduct> menuProducts);

        Task SendMenu(int id);
    }
}