using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PCCustomizer.Data;
using PCCustomizer.Models;
using PCCustomizer.Models.DTOs;
using System.Diagnostics;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 首頁我的菜單的商品相關服務的實作
    /// </summary>
    /// <seealso cref="CommunityToolkit.Mvvm.ComponentModel.ObservableObject" />
    /// <seealso cref="PCCustomizer.Services.IMenuService" />
    public class MenuService(AppDbContext dbContext, INotificationService notificationService) : ObservableObject, IMenuService
    {
        private List<MenuCategory> _menuCategories = [];
        public List<MenuCategory> MenuCategories
        {
            get => _menuCategories;
            private set
            {
                if (SetProperty(ref _menuCategories, value))
                {
                    OnStateChanged?.Invoke();
                }
            }
        }

        public event Action OnStateChanged;

        public async Task AddMenuCategory()
        {
            var count = await dbContext.MenuCategory.CountAsync(); // 使用非同步
            try
            {
                dbContext.MenuCategory.Add(new MenuCategory
                {
                    Name = $"菜單 {count + 1}",
                });
                var result = await dbContext.SaveChangesAsync(); // 使用非同步
                if (result != 0)
                {
                    notificationService.ShowSuccess($"菜單 {count + 1} 建立成功");
                }
                else
                {
                    notificationService.ShowError($"菜單 {count + 1} 建立失敗");
                }
                await GetMenuCategoriesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"新增菜單時發生錯誤: {ex.Message}");
                notificationService.ShowError($"菜單 {count + 1} 建立失敗");
            }
        }

        public async Task GetMenuCategoriesAsync()
        {
            try
            {
                var result = await dbContext.MenuCategory.Include(x => x.Menus).ToListAsync();
                MenuCategories = result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查詢分類資料時發生錯誤: {ex.Message}");
                MenuCategories = [];
            }
        }

        public async Task AddMenuProduct(MenuCategory menuCategory, MyCategoryDTO myCategoryDTO,
            MyProductDTO myProductDTO, int qty)
        {
            try
            {
                if (menuCategory == null || myCategoryDTO == null || myProductDTO == null) return;

                // *** 修正 1：檢查商品時，必須同時檢查 MenuCategoryId ***
                var existProduct = await dbContext.Menu.FirstOrDefaultAsync(x =>
                    x.ProductName == myProductDTO.RawText && x.MenuCategoryId == menuCategory.Id);

                if (existProduct != null)
                {
                    if (qty <= 0)
                    {
                        dbContext.Menu.Remove(existProduct);
                    }
                    else
                    {
                        existProduct.Qty = qty;
                    }
                }
                else if (qty > 0)
                {
                    // *** 修正 2：安全地取得關聯實體和順序 ***
                    var findCategory = await dbContext.Category.FirstOrDefaultAsync(x => x.CategoryId == myCategoryDTO.CategoryId);
                    var subcategory = await dbContext.Subcategory.FirstOrDefaultAsync(x => x.SubcategoryName == myProductDTO.SubcategoryName);
                    var product = await dbContext.Product.FirstOrDefaultAsync(x => x.RawText == myProductDTO.RawText);

                    var currentMenuCount = await dbContext.Menu.CountAsync(m => m.MenuCategoryId == menuCategory.Id);

                    dbContext.Menu.Add(new MenuProduct
                    {
                        MenuCategoryId = menuCategory.Id,
                        CategoryId = findCategory.CategoryId,
                        CateroyName = findCategory.CategoryName,
                        SubcategoryName = subcategory.SubcategoryName,
                        ProductName = product.RawText,
                        ProdctFullText = product.FullText,
                        ProductPrice = product.Price ?? 0,
                        Seq = currentMenuCount,
                        Qty = qty,
                    });
                }
                await dbContext.SaveChangesAsync(); // 使用非同步

                // 重新載入所有菜單並觸發 OnStateChanged
                await GetMenuCategoriesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"新增商品時發生錯誤: {ex.Message}");
                notificationService.ShowError($"商品新增失敗");
            }
        }

        public async Task<List<Dictionary<string, List<MenuProduct>>>> GetDictMyMenu(MenuCategory menuCategory)
        {
            try
            {
                if (menuCategory == null)
                {
                    return [];
                }

                var fineMenuCategory = await dbContext.MenuCategory.Include(x => x.Menus)
                    .FirstOrDefaultAsync(x => x.Id == menuCategory.Id);
                var menus = fineMenuCategory.Menus.OrderBy(x => x.CategoryId).ToList();
                var filterCategory = menus.DistinctBy(x => x.CategoryId).ToList();
                var result = new List<Dictionary<string, List<MenuProduct>>>();
                foreach (var category in filterCategory)
                {
                    var menuProductList = menus.Where(x => x.CategoryId == category.CategoryId)
                        .OrderBy(x => x.Seq).ToList();
                    result.Add(new Dictionary<string, List<MenuProduct>>
                    {
                        {
                            category.CateroyName,
                            menuProductList
                        }
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"主頁簡易查詢我的商品發生錯誤: {ex.Message}");
                return [];
            }
        }
    }
}

