# PCCustomizer(組電腦小幫手)

由於沒開發過桌面app，所以看看現在寫桌面app都用甚麼技術，因此就來學習這個`blazor hybird`
然後順便做一個小專案，證明自己的學習成果。

## 程式架構

DTO 抓取json的model

```
PCCustomizer/
│
├─ App/                     # MAUI App 主程式
│   ├─ App.xaml
│   └─ App.xaml.cs
│
├─ Components/              # 所有 Razor 元件（包含 Page 與 Layout）
│   ├─ Pages/               # Blazor 頁面
│   │   ├─ MainPage.razor
│   │   └─ ComputerList.razor
│   ├─ Layout/              # 共用 Layout
│   │   ├─ MainLayout.razor
│   │   └─ NavMenu.razor
│   └─ SharedComponents/    # 可重用元件 (Card, Table, Dialog...)
│       └─ ComputerCard.razor
│
├─ Services/                # 服務層
│   ├─ ComputerDataService.cs   # 抓 JSON / API
│   └─ ThemeService.cs          # 主題設定 (Microsoft.Maui.Storage)
│
├─ Models/                  # 資料模型
│   └─ ComputerPart.cs
│
├─ Data/                    # SQLite DB / migration
│   └─ AppDbContext.cs
│
├─ Resources/               # MAUI 資源
│   ├─ Fonts/
│   ├─ Images/
│   ├─ Splash/
│   ├─ AppIcon/
│   └─ Raw/                 # 放靜態 JSON 或其他 Raw 資料
│
├─ Converters/              # 可選：值轉換器（例如表格顯示格式）
│   └─ PriceConverter.cs
│
└─ MauiProgram.cs           # DI 註冊、MudBlazor 註冊、Service 註冊
```

###  各項核心說明

1. **Components**
   - **Pages** 
       * 放每個「可路由」頁面，例如首頁 `MainPage.razor`、電腦組件列表 `ComputerList.razor`
       * 每個頁面只負責組合元件與呈現資料

    - **Layout**
       * 放共用 Layout（例如導覽選單、標題欄、MudBlazor Drawer）
       * 全站共用，方便之後擴充

    - **SharedComponents**
       * 放可重複使用的 UI 元件，例如電腦卡片、對話框、表格
       * 讓 Page 保持乾淨、只組合元件

    - **_Imports.razor**：定義元件的「全局 using」，減少重複程式碼。
    - **Routes.razor**：定義應用程式的「導航機制」，將 URL 請求映射到對應的頁面元件。

2. **Services/**

   * 抓 JSON、存取 SQLite、管理主題或偏好設定
   * **全部透過 DI 注入**，方便在 Page 裡直接用 `@inject` 或建構子注入

3. **Models/**

   * 定義 JSON / DB Entity / DTO
   * 方便 Service 與 Page 共用

4. **Data/**

   * 如果以後用 SQLite，放 DbContext、Migration 檔
   * 對應 EF Core 設計模式

---


