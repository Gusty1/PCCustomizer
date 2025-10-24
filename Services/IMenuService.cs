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
        List<MenuCategory> MenuCategories { get;}

        event Action OnStateChanged;

        Task AddMenuCategory();
        
        Task GetMenuCategoriesAsync();

        Task AddMenuProduct(MenuCategory menuCategory, MyCategoryDTO myCategoryDTO,
            MyProductDTO myProductDTO, int qty);

        Task<List<Dictionary<string, List<MenuProduct>>>> GetDictMyMenu(MenuCategory menuCategory);
    }
}