// 檔案路徑: YourProject/Services/CategoryService.cs
using Microsoft.EntityFrameworkCore; // 需要引用這個來使用 Include 和 ThenInclude
using PCCustomizer.Data;
using PCCustomizer.Models;
using System.Diagnostics;

namespace PCCustomizer.Services
{
    public class CategoryService(AppDbContext dbContext) : ICategoryService
    {
        public async Task<List<Category>> GetCategoriesWithDetailsAsync()
        {
            try
            {
                // 這是 EF Core 的「預先載入 (Eager Loading)」功能
                // .Include(...): 告訴 EF Core，在查詢 Categories 的時候，請「一併載入」每一個 Category 關聯的 Subcategories 列表。
                // .ThenInclude(...): 接著，對於每一個載入的 Subcategory，請「再一併載入」它關聯的 Products 列表。
                // .ToListAsync(): 最後，將這個完整的、包含所有層級資料的查詢，非同步地執行並轉換成一個 List。
                return await dbContext.Category
                    .OrderBy(c => c.CategoryId)
                    .Include(c => c.Subcategories)
                    .ThenInclude(s => s.Products)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查詢分類資料時發生錯誤: {ex.Message}");
                // 如果查詢失敗，回傳一個空的列表，避免程式崩潰
                return new List<Category>();
            }
        }
    }
}