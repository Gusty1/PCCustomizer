# AGENTS.md — PCCustomizer 專案 Agent 執行指南

> 更新日期：2026-04-28
> 適用對象：所有在此專案中操作的 AI Agent（Claude Code 等）

---

## 1. 專案概覽

**PCCustomizer** 是一款 PC 零件比價輔助工具：從原價屋擷取商品資料，讓使用者自訂報價菜單，一鍵產生估價單。

| 項目 | 內容 |
|------|------|
| 框架 | .NET MAUI 10 + Blazor Hybrid |
| UI 套件 | MudBlazor 9 |
| 資料庫 | SQLite + EF Core 10（`EnsureCreatedAsync`，非 Migration） |
| 主要語言 | C# + Razor |
| 目標平台 | Windows（主要）、Android、iOS、macOS Catalyst |

---

## 2. 目錄結構

```
PCCustomizer/
├── MauiProgram.cs              # DI 容器設定（所有服務在這裡註冊）
├── App.xaml / App.xaml.cs      # MAUI App 入口
├── MainPage.xaml               # BlazorWebView 宿主
│
├── Components/
│   ├── BaseComponent.cs        # 所有 Razor 元件的基底（含 NotificationService 注入）
│   ├── _Imports.razor          # 全域 @using
│   ├── Routes.razor            # 路由定義
│   ├── General/                # 通用小元件
│   │   ├── ComputerInfoDialog.razor
│   │   └── PriceAlertText.razor
│   ├── Home/                   # 首頁專屬元件
│   │   ├── ProductContent.razor    # 商品列表（MudTabs）
│   │   └── CurrentMenuPopover.razor # 當前菜單 Popover
│   ├── Layout/                 # 版面配置
│   │   ├── MainLayout.razor    # 主版面（AppBar + 固定側邊欄）
│   │   ├── MyNavMenu.razor     # 側邊導覽（MudNavMenu）
│   │   └── LoadingOverlay.razor # 全域載入遮罩
│   └── Pages/
│       ├── Home.razor          # 首頁（商品瀏覽 + 菜單操作）
│       ├── Menu.razor          # 菜單管理頁
│       └── Setting.razor       # 設定頁
│
├── Services/
│   ├── IDataService.cs / DataService.cs          # 資料同步（Singleton）
│   ├── IMenuService.cs / MenuService.cs          # 菜單 CRUD（Scoped）
│   ├── ICategoryService.cs / CategoryService.cs  # 商品分類查詢（Scoped）
│   ├── ICoolPcService.cs / CoolPcService.cs      # 原價屋 HTTP（Transient via HttpClientFactory）
│   ├── IHardwareService.cs / HardwareService.cs  # 硬體掃描（Singleton）
│   ├── IThemeService.cs / ThemeService.cs        # 主題切換（Singleton）
│   ├── INotificationService.cs / NotificationService.cs # Snackbar 通知（Scoped）
│   └── IUpdateCheckService.cs / UpdateCheckService.cs   # GitHub 版本檢查（Transient）
│
├── Models/
│   ├── Category.cs / Subcategory.cs / Product.cs  # 商品資料（從網路 JSON 載入）
│   ├── MenuCategory.cs / MenuProduct.cs           # 使用者菜單資料
│   ├── DTOs/                                       # 資料傳輸物件
│   └── Hardware/                                   # 硬體資訊模型
│
└── Data/
    └── AppDbContext.cs          # EF Core DbContext（含 Fluent API）
```

---

## 3. 服務生命週期（重要）

| 服務 | 生命週期 | 說明 |
|------|---------|------|
| `DataService` | **Singleton** | 持有 `IsGlobalLoading`、`IsLoading` 跨元件狀態 |
| `ThemeService` | **Singleton** | 全域主題狀態 |
| `HardwareService` | **Singleton** | 硬體掃描結果快取 |
| `MenuService` | **Scoped** | 依賴 EF Core DbContext（Scoped） |
| `CategoryService` | **Scoped** | 依賴 EF Core DbContext（Scoped） |
| `NotificationService` | **Scoped** | 依賴 MudBlazor ISnackbar（Scoped） |
| `CoolPcService` | **Transient** | 透過 IHttpClientFactory 管理 |
| `UpdateCheckService` | **Transient** | 透過 IHttpClientFactory 管理 |

> **注意**：`DataService` 是 Singleton，但需要使用 Scoped 服務時（如 `INotificationService`、`AppDbContext`），必須透過 `serviceProvider.CreateScope()` 手動建立作用域，禁止直接持有 Scoped 服務的參考。

---

## 4. 資料庫關鍵規則

### Schema 變更協議

**每次修改 Model（新增/刪除/更名欄位）後，必須提醒使用者：**

> 「此次修改變更了資料庫 schema，請刪除本機的 PCCustomizer.db3 後重新啟動 app，
> 否則查詢會靜默失敗（EF Core 用 EnsureCreatedAsync，不支援 ALTER TABLE）。」

DB 檔案路徑（Windows）：
```
C:\Users\{使用者名稱}\AppData\Local\Packages\com.companyname.pccustomizer_cgsmvjbq0fw2p\LocalState\PCCustomizer.db3
```

### Entity 關係

- `Category` 1 → N `Subcategory`（Cascade Delete）
- `Subcategory` 1 → N `Product`（透過 `SubcategoryName` 字串關聯）
- `MenuCategory` 1 → N `MenuProduct`（Cascade Delete）
- `MenuProduct` 包含快照資料（`CategoryName`、`ProductName` 等字串），不是外鍵引用商品

---

## 5. 全域載入遮罩機制

```
啟動 → MainLayout.OnInitializedAsync → DataService.SeedDataIfNeededAsync()
         ↓
     SeedDataIfNeededAsync 開始 → IsGlobalLoading = true（遮罩顯示）
         ↓
     Home.razor 完成 DB 讀取 → DataService.SetGlobalLoading(false)（遮罩關閉）
```

- `DataService.IsGlobalLoading`（預設 `true`）：控制 `LoadingOverlay` 元件的顯示
- `DataService.IsLoading`：用於 UI 按鈕 disabled（避免重複觸發 SeedData）
- `LoadingOverlay` 是獨立元件，直接訂閱 `DataService`，**不會觸發整個 Layout 重新渲染**

---

## 6. 編寫 Razor 元件的規範

1. **所有頁面元件繼承 `BaseComponent`**，自動取得 `NotificationService` 與 `OpenExternalLink` 方法
2. **MainLayout 不繼承 `BaseComponent`**（它是 `LayoutComponentBase`），需自行處理相依服務
3. 使用 `MudBlazor` 元件時，注意參數大小寫：`Class` 而非 `class`、`Style` 而非 `style`
4. `MudSelect` 需要非同步操作時，使用 `Value` + `ValueChanged`，不可用 `@bind-Value`
5. `DrawerVariant.Permanent` 在 MudBlazor 9 **不存在**，請使用 `DrawerVariant.Persistent`

---

## 7. 已知技術債（待處理）

| # | 問題 | 位置 | 說明 |
|---|------|------|------|
| M-08 | `AddMenuProduct` 有 5-6 次獨立 DB 查詢 | `MenuService.cs:89` | 單次使用者互動觸發，效能影響有限 |
| P-01 | `HardwareService` 快取非原子性 | `HardwareService.cs` | 多元件同時呼叫可能觸發多次掃描，建議用 `SemaphoreSlim` |
| P-02 | `GetCategoriesWithDetailsAsync` 全量載入 | `CategoryService.cs` | 每次都載入全部商品，資料量大時為效能瓶頸 |
| S-01 | GitHub API 匿名請求速率限制 | `UpdateCheckService.cs` | 60 次/小時，建議快取上次檢查時間 |

---

## 8. 禁止事項

- **禁止在 Singleton 服務中直接注入 Scoped 服務**（會造成 Captive Dependency 問題）
- **禁止使用 `DrawerVariant.Permanent`**（MudBlazor 9 不支援）
- **禁止修改 Model 欄位後不提醒使用者刪除 DB 檔案**
- **禁止在 `BaseComponent` 子類別中重複注入 `INotificationService`**（基底類別已注入）
- **禁止在 `MainLayout` 中繼承 `BaseComponent`**（`LayoutComponentBase` 不相容）
- **NavMenu.razor 已移除**，側邊導覽請使用 `MyNavMenu.razor`

---

## 9. 常見 Agent 操作場景

### 新增一個 Service

1. 建立 `Services/IXxxService.cs` 介面
2. 建立 `Services/XxxService.cs` 實作
3. 在 `MauiProgram.cs` 的服務註冊區加入 DI 設定
4. 確認生命週期是否與依賴項目相符（避免 Captive Dependency）

### 新增一個頁面

1. 在 `Components/Pages/` 建立 `Xxx.razor`
2. 加上 `@page "/xxx"` 路由指令
3. 繼承 `@inherits BaseComponent`
4. 在 `MyNavMenu.razor` 加入對應的 `MudNavLink`

### 修改 EF Core Model

1. 修改 `Models/` 下的對應 Model
2. **必須提醒使用者刪除本機 PCCustomizer.db3**
3. 在 `AppDbContext.cs` 更新 `OnModelCreating` 的 Fluent API 設定（如有必要）
