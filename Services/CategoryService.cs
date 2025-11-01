﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PCCustomizer.Data;
using PCCustomizer.Models;
using PCCustomizer.Models.DTOs;
using System.Diagnostics;

namespace PCCustomizer.Services
{
    /// <summary>
    /// 首頁原價物的商品相關服務的實作
    /// </summary>
    /// <seealso cref="CommunityToolkit.Mvvm.ComponentModel.ObservableObject" />
    /// <seealso cref="PCCustomizer.Services.ICategoryService" />
    public class CategoryService(AppDbContext dbContext) : ObservableObject, ICategoryService
    {
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

        public event Action OnStateChanged;

        public async Task<List<MyCategoryDTO>> GetCategoriesWithDetailsAsync(MenuCategory menuCategory)
        {
            try
            {
                IsLoading = true;
                // 這是 EF Core 的「預先載入 (Eager Loading)」功能
                // .Include(...): 告訴 EF Core，在查詢 Categories 的時候，請「一併載入」每一個 Category 關聯的 Subcategories 列表。
                // .ThenInclude(...): 接著，對於每一個載入的 Subcategory，請「再一併載入」它關聯的 Products 列表。
                // .ToListAsync(): 最後，將這個完整的、包含所有層級資料的查詢，非同步地執行並轉換成一個 List。
                var result = await dbContext.Category
                    .OrderBy(c => c.CategoryId)
                    .Include(c => c.Subcategories)
                    .ThenInclude(s => s.Products)
                    .AsSplitQuery()
                    .ToListAsync();
                var myCategoryDTOList = new List<MyCategoryDTO>();
                foreach (var category in result)
                {
                    var mySubcategoryDTOList = new List<MySubcategoryDTO>();
                    foreach (var subcategory in category.Subcategories)
                    {
                        var myProductDTOList = new List<MyProductDTO>();
                        foreach (var product in subcategory.Products)
                        {
                            var findProduct = menuCategory != null ? menuCategory.MenuProducts.FirstOrDefault(x => x.ProductName == product.RawText) : null;
                            myProductDTOList.Add(new MyProductDTO
                            {
                                Index = product.Index,
                                SubcategoryName = product.SubcategoryName,
                                Group = product.Group,
                                Price = product.Price,
                                Markers = product.Markers,
                                RawText = product.RawText,
                                FullText = product.FullText,
                                ImgUrl = product.ImgUrl,
                                ProductUrl = product.ProductUrl,
                                Details = product.Details,
                                Qty = findProduct == null ? 0 : findProduct.Qty
                            });
                        }
                        var subcategoryQty = menuCategory != null ? menuCategory.MenuProducts.Where(x => x.SubcategoryName == subcategory.SubcategoryName)
                            .Sum(k => k.Qty) : 0;
                        mySubcategoryDTOList.Add(new MySubcategoryDTO
                        {
                            CategoryId = subcategory.CategoryId,
                            SubcategoryName = subcategory.SubcategoryName,
                            Products = myProductDTOList,
                            Qty = subcategoryQty
                        });
                    }
                    myCategoryDTOList.Add(new MyCategoryDTO
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.CategoryName,
                        Summary = category.Summary,
                        Subcategories = mySubcategoryDTOList
                    });
                }

                return myCategoryDTOList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查詢分類資料時發生錯誤: {ex.Message}");
                IsLoading = false;
                return [];
            }
        }
    }
}