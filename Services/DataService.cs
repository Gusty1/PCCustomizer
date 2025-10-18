using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Newtonsoft.Json;
using PCCustomizer.Data;
using PCCustomizer.Models;
using PCCustomizer.Models.DTOs;
using System.Diagnostics;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 取得商品資料的服務
    /// </summary>
    /// <seealso cref="IDataService" />
    public class DataService(HttpClient httpClient, AppDbContext dbContext, ISnackbar snackbar) : IDataService
    {
        private const string ProductDataUrl = "https://gusty1.github.io/Database/coolPC/product.json";

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnStateChanged?.Invoke(); }
        }
        public event Action OnStateChanged;

        public async Task SeedDataIfNeededAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                // 確保資料庫和資料表結構都存在
                await dbContext.Database.EnsureCreatedAsync();

                //TODO 記得移除
                return;

                // 遵循「從下往上」的順序，清空所有資料
                Debug.WriteLine("正在清空 Product 資料表...");
                await dbContext.Product.ExecuteDeleteAsync();
                Debug.WriteLine("正在清空 Subcategories 資料表...");
                await dbContext.Subcategory.ExecuteDeleteAsync();
                Debug.WriteLine("正在清空 Categories 資料表...");
                await dbContext.Category.ExecuteDeleteAsync();

                Debug.WriteLine("所有相關資料表已清空，開始從網路獲取新資料...");
                var jsonResponse = await httpClient.GetStringAsync(ProductDataUrl);
                var parsedJson = JsonConvert.DeserializeObject<List<CategoryDTO>>(jsonResponse);
                if (parsedJson == null || parsedJson.Count == 0)
                {
                    Debug.WriteLine("從 JSON 讀取的資料為空。");
                    return;
                }

                var categoriesForDb = new List<Category>();
                Debug.WriteLine($"JSON 解析成功，包含 {parsedJson.Count} 個主分類。開始轉換為資料庫模型...");

                foreach (var jsonCategory in parsedJson)
                {
                    var newDbCategory = new Category
                    {
                        CategoryId = int.Parse(jsonCategory.CategoryId),
                        CategoryName = jsonCategory.CategoryName,
                        Summary = jsonCategory.Summary,
                    };

                    foreach (var jsonSubcategory in jsonCategory.Subcategories)
                    {
                        var newDbSubcategory = new Subcategory
                        {
                            CategoryId = newDbCategory.CategoryId,
                            SubcategoryName = jsonSubcategory.Name,
                        };

                        foreach (var jsonProduct in jsonSubcategory.Products)
                        {
                            var newDbProduct = new Product
                            {
                                SubcategoryName = newDbSubcategory.SubcategoryName,
                                Index = jsonProduct.Index,
                                Group = jsonProduct.Group,
                                Brand = jsonProduct.Brand,
                                Specs = jsonProduct?.Specs ?? [],
                                Price = jsonProduct.Price,
                                Markers = jsonProduct?.Markers ?? [],
                                RawText = jsonProduct.RawText,
                            };
                            newDbSubcategory.Products.Add(newDbProduct);
                        }
                        newDbCategory.Subcategories.Add(newDbSubcategory);
                    }
                    categoriesForDb.Add(newDbCategory);
                }
                Debug.WriteLine($"資料轉換完成，準備將 {categoriesForDb.Count} 個主分類及其所有子項寫入資料庫...");

                await dbContext.Category.AddRangeAsync(categoriesForDb);
                await dbContext.SaveChangesAsync();

                Debug.WriteLine("資料寫入完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化資料時發生嚴重錯誤: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"--> 內部錯誤訊息: {ex.InnerException.Message}");
                }
                snackbar.Add(@"資料更新失敗，請檢查網路連線或稍後再試，
                                若還有問題請聯絡我", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}