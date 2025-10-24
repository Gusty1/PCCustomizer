using Microsoft.EntityFrameworkCore;
using PCCustomizer.Models;

namespace PCCustomizer.Data
{
    /// <summary>
    /// 系統會根據這個檔案執行時，檢查是否有對應的資料庫
    /// </summary>
    /// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Product { get; set; }

        public DbSet<Category> Category { get; set; }

        public DbSet<Subcategory> Subcategory { get; set; }

        public DbSet<MenuCategory> MenuCategory { get; set; }

        public DbSet<MenuProduct> Menu { get; set; }
    }
}