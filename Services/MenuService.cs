using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PCCustomizer.Data;
using PCCustomizer.Models;
using PCCustomizer.Models.DTOs;
using PCCustomizer.Tools;
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
        private readonly static string CoolPC = "https://www.coolpc.com.tw/tmp/";

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                // 當值改變時，更新屬性並觸發 OnChange 事件通知 UI
                if (SetProperty(ref _isLoading, value))
                {
                    OnStateChanged?.Invoke();
                }
            }
        }

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
                var result = await dbContext.MenuCategory.Include(x => x.MenuProducts).ToListAsync();
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

                var existProduct = await dbContext.MenuProduct.FirstOrDefaultAsync(x =>
                    x.ProductName == myProductDTO.RawText && x.MenuCategoryId == menuCategory.Id);

                if (existProduct != null)
                {
                    if (qty <= 0)
                    {
                        dbContext.MenuProduct.Remove(existProduct);
                    }
                    else
                    {
                        existProduct.Qty = qty;
                    }
                }
                else if (qty > 0)
                {
                    var findCategory = await dbContext.Category.FirstOrDefaultAsync(x => x.CategoryId == myCategoryDTO.CategoryId);
                    var subcategory = await dbContext.Subcategory.FirstOrDefaultAsync(x => x.SubcategoryName == myProductDTO.SubcategoryName);
                    var product = await dbContext.Product.FirstOrDefaultAsync(x => x.RawText == myProductDTO.RawText);
                    var currentMenuCategory = await dbContext.MenuCategory.FirstOrDefaultAsync(x => x.Id == menuCategory.Id);
                    if (currentMenuCategory != null)
                    {
                        currentMenuCategory.ReviseDate = DateTime.Now;
                    }

                    dbContext.MenuProduct.Add(new MenuProduct
                    {
                        MenuCategoryId = menuCategory.Id,
                        CategoryId = findCategory.CategoryId,
                        CateroyName = findCategory.CategoryName,
                        SubcategoryName = subcategory.SubcategoryName,
                        ProductName = product.RawText,
                        ProdctFullText = product.FullText,
                        ProductPrice = product.Price ?? 0,
                        Qty = qty,
                    });
                }
                await dbContext.SaveChangesAsync();

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

                var fineMenuCategory = await dbContext.MenuCategory.Include(x => x.MenuProducts)
                    .FirstOrDefaultAsync(x => x.Id == menuCategory.Id);
                var menus = fineMenuCategory.MenuProducts.OrderBy(x => x.CategoryId).ToList();
                var filterCategory = menus.DistinctBy(x => x.CategoryId).ToList();
                var result = new List<Dictionary<string, List<MenuProduct>>>();
                foreach (var category in filterCategory)
                {
                    var menuProductList = menus.Where(x => x.CategoryId == category.CategoryId).ToList();
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

        public async Task<List<MyMenuCategoryDTO>> GetMyMenuCtegoryDTOs()
        {
            try
            {
                var myMenuCategories = await dbContext.MenuCategory.Include(x => x.MenuProducts).ToListAsync();
                var result = new List<MyMenuCategoryDTO>();
                foreach (var menuCategory in myMenuCategories)
                {
                    result.Add(new MyMenuCategoryDTO
                    {
                        Id = menuCategory.Id,
                        Name = menuCategory.Name,
                        ReviseDate = menuCategory.ReviseDate,
                        IsSend = menuCategory.IsSend,
                        HtmUrl = menuCategory.HtmUrl,
                        PngUrl = menuCategory.PngUrl,
                        MyMenuProducts = await GetDictMyMenu(menuCategory)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查詢我的菜單全部發生錯誤: {ex.Message}");
                return [];
            }
        }

        public async Task DeleteMenuCategory(int id)
        {
            try
            {
                var findMenuCategory = await dbContext.MenuCategory.FirstOrDefaultAsync(x => x.Id == id);
                if (findMenuCategory != null)
                {
                    dbContext.MenuCategory.Remove(findMenuCategory);
                }
                var result = await dbContext.SaveChangesAsync();
                if (result > 0)
                {
                    notificationService.ShowSuccess($"刪除 {findMenuCategory.Name} 成功");
                }
                else
                {
                    notificationService.ShowError($"刪除 {findMenuCategory.Name} 失敗");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刪除菜單時發生錯誤: {ex.Message}");
                notificationService.ShowError($"刪除菜單失敗");
            }
        }

        public async Task UpdateMenuCategoryAsync(MyMenuCategoryDTO myMenuCategoryDTO, List<MenuProduct> menuProducts)
        {
            try
            {
                if (myMenuCategoryDTO == null) return;

                var findMenuCategory = await dbContext.MenuCategory.FirstOrDefaultAsync(x => x.Id == myMenuCategoryDTO.Id);
                var findMenuProducts = await dbContext.MenuProduct.Where(x => x.MenuCategoryId == myMenuCategoryDTO.Id).ToListAsync();
                if (findMenuCategory != null)
                {
                    findMenuCategory.Name = myMenuCategoryDTO.Name;
                    findMenuCategory.ReviseDate = DateTime.Now;
                    foreach (var editCategory in myMenuCategoryDTO.MyMenuProducts)
                    {
                        foreach (var (key, value) in editCategory)
                        {
                            foreach (var editProduct in value)
                            {
                                if (editProduct.Qty <= 0) continue;
                                var fineMenuProduct = findMenuProducts.FirstOrDefault(x => x.ProductName == editProduct.ProductName
                                && x.CateroyName == key);
                                if (fineMenuProduct != null)
                                {
                                    fineMenuProduct.Qty = editProduct.Qty;
                                }
                            }
                        }
                    }
                }
                if (menuProducts.Any())
                {
                    var productIds = menuProducts.Select(x => x.Id).ToList();
                    var deleteList = findMenuProducts.Where(x => productIds.Contains(x.Id)).ToList();
                    dbContext.MenuProduct.RemoveRange(deleteList);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新菜單資料時發生錯誤: {ex.Message}");
                notificationService.ShowError($"更新菜單失敗");
            }
        }

        public async Task SendMenu(int id)
        {
            try
            {
                IsLoading = true;
                var findMenu = await dbContext.MenuCategory.Include(x => x.MenuProducts).FirstOrDefaultAsync(x => x.Id == id);
                var payloadStr = CoolPcWebUtility.BuildPayLoad(findMenu.MenuProducts).Trim();
                var cookie = await CoolPcWebUtility.GetCoolPcSessionIdAsync();
                string htmUrl = "";
                string pngUrl = "";
                //由於網址和圖片的名稱是用js動態產生的，只靠C#不能直接取得，第一次先取得網址
                //第二次再把網址和圖片帶入
                for (int i = 0; i < 2; i++)
                {
                    var payload = new Dictionary<string, string>
                    {
                        { "pngdoc", payloadStr },
                        { "fdoc", payloadStr+"<@>"},
                        { "fname", htmUrl},
                        { "iname", pngUrl}
                    };
                    var result = await CoolPcWebUtility.SendAndParseEstimateAsync(cookie, payload);
                    if (i == 0)
                    {
                        htmUrl = result.GetValueOrDefault().HtmFilename != null ? CoolPC + result.GetValueOrDefault().HtmFilename : null;
                        pngUrl = result.GetValueOrDefault().PngFilename != null ? CoolPC + result.GetValueOrDefault().PngFilename : null;
                    }
                }
                findMenu.HtmUrl = htmUrl;
                findMenu.PngUrl = pngUrl;
                findMenu.IsSend = true;
                findMenu.ReviseDate = DateTime.Now;

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"傳送菜單資料時發生錯誤: {ex.Message}");
                notificationService.ShowError($"傳送菜單失敗");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

