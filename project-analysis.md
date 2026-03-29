# PCCustomizer 專案分析報告

> 生成日期：2026-03-27
> 最後更新：2026-03-28（已完成六輪修正，所有 LOW 技術債已處理完畢；第 10 節 UI 設計審查所有問題均已修復）
> 分析範圍：完整專案架構、程式碼品質、依賴管理、UI 設計、潛在問題

---

## 目錄

1. [專案概覽](#1-專案概覽)
2. [架構分析：模組分層](#2-架構分析模組分層)
3. [依賴管理分析](#3-依賴管理分析)
4. [潛在 Bug 與壞味道](#4-潛在-bug-與壞味道)
5. [UI 元件設計分析](#5-ui-元件設計分析)
6. [安全性評估](#6-安全性評估)
7. [效能分析](#7-效能分析)
8. [剩餘技術債（待處理）](#8-剩餘技術債待處理)
9. [未來建議](#9-未來建議)
10. [UI 設計審查](#10-ui-設計審查)

---

## 1. 專案概覽

| 項目 | 內容 |
|------|------|
| **框架** | .NET MAUI 10.0.41 + Blazor Hybrid |
| **UI 套件** | MudBlazor 9.0.0 |
| **資料庫** | SQLite + EF Core 10.0.3 |
| **平台** | Windows / Android / iOS / macOS Catalyst / Tizen |
| **程式碼行數** | ~2,700 行（C# + Razor） |
| **服務介面** | 7 個 |
| **NuGet 套件** | 15 個 |

### 應用程式定位

PC 零件比價輔助工具，從原價屋擷取商品資料後，讓使用者自訂報價菜單並一鍵產生估價單。
核心業務流程：**資料同步 → 商品瀏覽 → 菜單管理 → 估價單產生**

---

## 2. 架構分析：模組分層

### 2.1 現行目錄結構

```
PCCustomizer/
├── App.xaml / App.xaml.cs          # MAUI App 入口
├── MauiProgram.cs                   # DI 容器與服務組態
├── MainPage.xaml                    # BlazorWebView 宿主
├── AppConstants.cs                  # 常數定義 (URL、預設值)
│
├── Components/                      # UI 層 (Razor)
│   ├── BaseComponent.cs             # 共用基底
│   ├── _Imports.razor               # 全域 @using
│   ├── Routes.razor                 # 路由定義
│   ├── General/                     # 通用 UI 小元件
│   ├── Home/                        # 首頁專屬元件
│   ├── Layout/                      # 版面配置
│   └── Pages/                       # 頁面 (Home / Menu / Setting)
│
├── Services/                        # 業務邏輯層
│   ├── I*Service.cs                 # 介面定義
│   ├── *Service.cs                  # 實作
│   ├── ICoolPcService.cs            # 原價屋 HTTP 服務介面
│   └── CoolPcService.cs             # 原價屋 HTTP 服務實作（Session 取得 + POST 發送 + HTML 解析 + Payload 組裝）
│
├── Models/                          # 資料模型層
│   ├── *.cs                         # EF 實體模型
│   ├── DTOs/                        # 資料傳輸物件
│   └── Hardware/                    # 硬體資訊模型
│
├── Data/
│   └── AppDbContext.cs              # EF Core DbContext（含 Fluent API 設定）
│
├── Platforms/                       # 各平台特定程式碼
└── Resources/                       # 靜態資源
```

### 2.2 分層評估

| 層次 | 目前狀態 | 評分 | 說明 |
|------|---------|------|------|
| **UI 層** (Components) | 良好 | A- | 依功能分資料夾，BaseComponent 提供共用邏輯，Popover 已抽離為獨立元件 |
| **業務邏輯層** (Services) | 良好 | A- | 介面/實作分離，DI 完整，CoolPcService 透過 IHttpClientFactory 注入 |
| **資料模型層** (Models) | 良好 | A- | 實體與 DTO 分離，Hardware 模型另置子目錄 |
| **資料存取層** (Data) | 良好 | B+ | AppDbContext 已加入 Fluent API：Cascade Delete、MaxLength 設定 |
| **工具層** (Tools) | — | — | 已整合為 `Services/CoolPcService.cs`，透過 DI 管理，不再有靜態工具類 |

### 2.3 服務：CoolPcService（已重構）

原 `Tools/CoolPcWebUtility.cs` 靜態類別已重構為可注入服務：

- `Services/ICoolPcService.cs` — 介面定義（`GetSessionIdAsync`、`BuildPayload`、`SendAndParseEstimateAsync`）
- `Services/CoolPcService.cs` — 實作，由 `IHttpClientFactory` 管理 HttpClient，支援 DI 注入與單元測試

### 2.4 UI：Home.razor（已精簡）

「當前菜單 Popover」已抽取為 `Components/Home/CurrentMenuPopover.razor` 獨立元件，Home.razor 縮減約 80 行。現行元件行數約 160 行。

### 2.5 服務生命週期設計

| 服務 | 生命週期 | 合理性 |
|------|---------|--------|
| `ThemeService` | Singleton | 正確（全域主題狀態） |
| `HardwareService` | Singleton | 正確（硬體掃描快取） |
| `CategoryService` | Scoped | 正確（EF DbContext Scoped） |
| `MenuService` | Scoped | 正確 |
| `NotificationService` | Scoped | 可接受 |
| `DataService` | Transient（HttpClient 工廠） | 正確 |
| `UpdateCheckService` | Transient（HttpClient 工廠） | 正確 |

---

## 3. 依賴管理分析

### 3.1 目前套件清單

| 套件 | 目前版本 | 狀態 | 備註 |
|------|---------|------|------|
| `Microsoft.Maui.Controls` | 10.0.41 | 最新 | |
| `Microsoft.EntityFrameworkCore` | 10.0.3 | 最新 | |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.3 | 最新 | |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.3 | 最新 | |
| `Microsoft.AspNetCore.Components.WebView.Maui` | 10.0.41 | 最新 | |
| `Microsoft.Extensions.Http` | 10.0.3 | 最新 | |
| `Microsoft.Extensions.Logging.Debug` | 10.0.3 | 最新 | |
| `MudBlazor` | 9.0.0 | 最新 | |
| `MudBlazor.ThemeManager` | 4.0.0 | 最新 | |
| `CommunityToolkit.Mvvm` | 8.4.0 | 最新 | |
| ~~`Newtonsoft.Json`~~ | ~~13.0.4~~ | **已移除** | 已統一改用 `System.Text.Json`（內建於 .NET runtime） |
| ~~`HtmlAgilityPack`~~ | ~~1.12.4~~ | **已移除** | 確認未使用，已移除 |
| `System.Management` | 10.0.3 | 最新 | WMI，僅限 Windows |
| `System.Text.Encoding.CodePages` | 10.0.3 | 最新 | Big5 編碼支援 |
| ~~`Microsoft.Maui.Controls.Compatibility`~~ | ~~10.0.41~~ | **已移除** | 新 MAUI 10 專案不需要此 Xamarin 相容層 |

### 3.2 已移除的冗餘依賴

| 套件 | 移除原因 |
|------|---------|
| `HtmlAgilityPack` | 已改用 `System.Text.RegularExpressions` 解析 HTML，確認無其他引用後移除 |
| `Microsoft.Maui.Controls.Compatibility` | Xamarin.Forms 相容層，全新 MAUI 10 專案不需要 |
| `Newtonsoft.Json` | 已統一改用 `System.Text.Json`（內建於 .NET runtime），減少應用程式體積約 1MB |

---

## 4. 潛在 Bug 與壞味道

> 已修正項目：4.1（csproj 敏感資訊）、4.2（async void 例外）、4.3（SendMenu 驗證）、4.4（OrderBy 賦值）、4.5（防禦性取消訂閱）、4.6（方法名稱拼字）、4.7（GetDictMyMenu 回傳型別）、4.9（重複 API 請求）、4.10（AppDbContext Fluent API）、4.11（hardcoded 路徑）、4.12（重複 User-Agent）、4.13（CoolPcWebUtility 靜態 HttpClient）均已完成修正，以下僅保留尚待處理項目。

---

### 4.8 MEDIUM：`AddMenuProduct` 存在 N+1 查詢問題

**檔案**：`Services/MenuService.cs:90-146`

每次新增一個商品時，會觸發：
1. `FirstOrDefaultAsync` 查詢 `MenuProduct`
2. `FirstOrDefaultAsync` 查詢 `Category`
3. `FirstOrDefaultAsync` 查詢 `Subcategory`
4. `FirstOrDefaultAsync` 查詢 `Product`
5. `FirstOrDefaultAsync` 查詢 `MenuCategory`
6. `SaveChangesAsync`

共 **5-6 次**獨立 DB 查詢。由於此操作為單次使用者互動觸發（非批次），效能影響有限，但可考慮合併部分查詢。

---

## 5. UI 元件設計分析

### 5.1 元件架構評估

| 元件 | 行數 | 評估 |
|------|------|------|
| `Home.razor` | ~160 | 良好（Popover 已抽離至 `CurrentMenuPopover.razor`） |
| `CurrentMenuPopover.razor` | ~95 | 良好（新增獨立元件） |
| `Menu.razor` | ~320 | 合理，但表格列渲染可考慮抽取元件 |
| `ProductContent.razor` | ~230 | 合理 |
| `Setting.razor` | ~107 | 良好 |
| `ComputerInfoDialog.razor` | ~99 | 良好 |
| `MainLayout.razor` | ~73 | 良好 |

### 5.2 嵌套過深問題

**檔案**：`Components/Pages/Home.razor`（Popover 表格）
**檔案**：`Components/Pages/Menu.razor`（菜單表格）

兩處都有雙層嵌套（`@foreach` + `@for`），配合 `@if (i == 0)` 條件渲染 `rowspan`，邏輯複雜且難以維護。（已由三層改為兩層，完成 4.7 重構後）

建議進一步封裝為 `MenuTableRows.razor` 元件，接收 `Dictionary<string, List<MenuProduct>>` 參數。

### 5.3 MudSelect 的 `Value` 屬性手動管理

**檔案**：`Components/Pages/Home.razor`

使用 `Value` + `ValueChanged` 而非 `@bind-Value`，是因為需要在值變更時執行非同步操作，屬於**正確且必要**的 MudBlazor 使用方式。

### 5.4 `Menu.razor` 中編輯狀態管理

目前 `DeleteProducts` 是 `List<MenuProduct>`，在多個地方都操作：`StartEditing`、`CancelEdit`、`SaveChanges`、`DeleteProduct`、`SendMenu`。

若用戶在編輯 A 菜單期間快速點擊其他菜單的編輯按鈕，`currentlyEditingCardId` 只能追蹤一個，但 `DeleteProducts` 不會清空，可能產生狀態污染。（目前 UI 上因同一時間只能編輯一個 Card，風險較低，但設計上不夠嚴謹。）

---

## 6. 安全性評估

| 項目 | 風險等級 | 說明 |
|------|---------|------|
| GitHub API 無驗證 | LOW | 匿名請求速率限制 60次/小時，影響更新檢查，見未來建議 9.1 |
| SQL Injection | 無風險 | EF Core 參數化查詢 |
| XSS | 無風險 | Blazor 自動 HTML 編碼 |
| Cookie 傳輸 | 低風險 | PHPSESSID 在 HTTPS 下傳輸，原價屋若支援 HTTPS 則安全 |
| Hardcoded URL | 資訊暴露 | ProductDataUrl、CoolPC URL 在原始碼中可見（低風險） |

---

## 7. 效能分析

### 7.1 資料載入效能

| 操作 | 查詢次數 | 評估 |
|------|---------|------|
| `GetCategoriesWithDetailsAsync` | 3（AsSplitQuery） | 良好 |
| `GetDictMyMenu` | 1 (Include) | 良好 |
| `AddMenuProduct` | 5-6 | 可改善 |
| `GetMyMenuCategoryDTOs` | 1（已修正 N+1，改為記憶體處理） | 良好 |

### 7.2 `CategoryService.GetCategoriesWithDetailsAsync` 全量載入

每次選擇菜單或更新商品，都會重新從 DB 載入**所有**分類、子分類、商品的完整資料。若資料量大（例如數千個商品），這會是效能瓶頸。

目前對於一般使用規模（原價屋商品數量）影響有限，但架構上不夠靈活。

### 7.3 硬體掃描 Thread Safety

**檔案**：`Services/HardwareService.cs`

```csharp
// 快取機制
if (_cachedInfo != null) return _cachedInfo;
_cachedInfo = await Task.Run(() => { ... });
```

在 Scoped 生命週期的 DI 容器中，`HardwareService` 是 Singleton，快取邏輯是安全的。但若多個 Blazor 元件幾乎同時呼叫 `ScanComputerInfoAsync()`，可能觸發多次重複掃描（非原子性 check-then-set）。

---

## 8. 剩餘技術債（待處理）

所有 CRITICAL / HIGH / MEDIUM / LOW 問題均已完成修正。
以下項目為架構延伸改善建議（非必要，視未來需求決定是否執行）。

| # | 問題 | 檔案 | 說明 |
|---|------|------|------|
| M-08 | `AddMenuProduct` 存在 5-6 次獨立 DB 查詢 | `MenuService.cs` | 單次互動觸發，效能影響有限，可考慮合併查詢 |

---

## 9. 未來建議

本節整合尚未實作的架構改善方向，按建議優先度排序。

### 9.1 GitHub API 速率限制防護

`UpdateCheckService` 目前使用匿名請求，上限為 60 次/小時。若未來使用者較多，或在 CI/CD 環境中頻繁執行，可能觸發速率限制。

**建議：**
- 短期：快取上次檢查結果，設定最短檢查間隔（例如每小時最多一次）
- 長期：允許使用者設定 GitHub Personal Access Token（速率上限提升至 5,000 次/小時）

---

### 9.2 HardwareService Thread Safety（非原子性快取）

```csharp
// 非原子性 check-then-set，多元件同時呼叫時可能觸發多次掃描
if (_cachedInfo != null) return _cachedInfo;
_cachedInfo = await Task.Run(() => { ... });
```

**建議**：使用 `SemaphoreSlim(1, 1)` 或 `Lazy<Task<T>>` 保證只執行一次掃描：

```csharp
private SemaphoreSlim _scanLock = new(1, 1);
await _scanLock.WaitAsync();
try { /* 掃描邏輯 */ }
finally { _scanLock.Release(); }
```

---

## 10. UI 設計審查

> 審查時間：2026-03-28
> 最後更新：2026-03-28（所有 UI 問題均已修復完畢）
> 審查範圍：所有 Razor UI 元件（MainLayout、Home、Menu、Setting、ProductContent、CurrentMenuPopover、PriceAlertText、ComputerInfoDialog、MyNavMenu）

### UI 審查彙總

| # | 檔案 | 問題 | 嚴重度 | 狀態 |
|---|------|------|--------|------|
| U-01 | `MainLayout.razor` | 雙重 Overlay 衝突，Home.razor 的 Overlay 永遠不顯示；樣式水平排列不符 UX 慣例 | HIGH | **已修復** — 移除 Home.razor Overlay、統一為垂直置中樣式、移除隱藏 Body 的條件判斷 |
| U-02 | `ProductContent.razor` | 第三欄表頭（數量欄）只有 Razor 注解，不顯示任何文字 | MEDIUM | **已修復** — 改為顯示「**數量**」標題 |
| U-03 | `Menu.razor` | 菜單卡片表格價格欄未套用千分位格式，與其他位置顯示不一致 | MEDIUM | **已修復** — 兩處均改為 `.ToString("N0")` |
| U-04 | `Menu.razor` | 卡片高度固定 600px，商品少時下方空白過多 | LOW | **已修復** — 改為 `min-height: 300px; max-height: 600px` |
| U-05 | `Setting.razor` | 冗餘 `md/sm` 響應式屬性；`MudText` 使用小寫 `class` 而非 Blazor 參數 `Class` | LOW | **已修復** — 移除多餘屬性；`class` 改為 `Class` |
| U-06 | `Setting.razor` | 「深色模式/意見回饋」與「版本資訊」區段缺少視覺分隔 | LOW | **已修復** — 加入 `MudDivider` 分隔 |
| U-07 | `Home.razor` | 「新增菜單」按鈕缺少 `Color`，顯示預設灰色，與整體風格不一致 | LOW | **已修復** — 補上 `Color="Color.Primary"` |
| U-08 | `MyNavMenu.razor` | 檔案以非 UTF-8 編碼儲存，中文字元顯示亂碼 | HIGH | **已修復** — 重新以 UTF-8 儲存 |
| U-09 | `ComputerInfoDialog.razor` | 對話框無可見「×」關閉按鈕，只能依賴 Esc 鍵 | LOW | **已修復** — 加入 `CloseButton="true"` |

---

## 附錄：關鍵檔案路徑

| 分類 | 路徑 | 備註 |
|------|------|------|
| 專案設定 | `PCCustomizer.csproj` | |
| DI 容器 | `MauiProgram.cs` | |
| 資料庫設定 | `Data/AppDbContext.cs` | 含 Fluent API |
| 分類服務 | `Services/CategoryService.cs` | |
| 資料同步服務 | `Services/DataService.cs` | |
| 菜單服務 | `Services/MenuService.cs` | |
| 原價屋 HTTP 服務 | `Services/CoolPcService.cs` | 取代原 CoolPcWebUtility.cs |
| 硬體服務 | `Services/HardwareService.cs` | |
| 更新檢查服務 | `Services/UpdateCheckService.cs` | |
| 首頁 | `Components/Pages/Home.razor` | |
| 菜單頁 | `Components/Pages/Menu.razor` | |
| 設定頁 | `Components/Pages/Setting.razor` | |
| 當前菜單 Popover | `Components/Home/CurrentMenuPopover.razor` | 從 Home.razor 抽離 |
| 商品列表元件 | `Components/Home/ProductContent.razor` | |
| 主版面 | `Components/Layout/MainLayout.razor` | |
