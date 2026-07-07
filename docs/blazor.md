# Blazor 完整學習筆記（.NET 10）

[官方文件](https://learn.microsoft.com/zh-tw/aspnet/core/blazor/?view=aspnetcore-10.0)

> 本文基於 .NET 10 官方文件整理，以 **Blazor Server（企業 ERP/MIS/工廠系統的推薦選擇）** 為主要情境，工廠 IT 開發視角撰寫。其他模式（WASM、Auto）的比較僅作為選型參考，不深入說明。

---

## 一、Blazor 是什麼

Blazor 是 .NET 的前端 Web 框架，讓你用 C# 寫 UI，而不是 JavaScript。核心特性：

- 用 C# 寫出豐富的互動式 UI
- 伺服器端與客戶端的應用邏輯可以共用同一套 .NET 程式碼
- 渲染出標準的 HTML + CSS，支援所有瀏覽器

---

## 二、三種模式比較

### 1. Blazor Server（`InteractiveServer`）

* **運作原理：** 應用程式完全跑在**伺服器端**，瀏覽器只是高階顯示器。使用者的每次點擊透過 **SignalR（WebSocket）** 傳回伺服器，伺服器算完差異再傳回來局部更新畫面。
* **白話比喻：** 雲端串流遊戲——畫面在遠端跑，你只負責傳操作指令和看畫面。

### 2. Blazor WebAssembly（`InteractiveWebAssembly`）

* **運作原理：** 應用程式完全跑在**瀏覽器**裡。第一次進入時下載整個 .NET Runtime 和 DLL，之後所有邏輯都在瀏覽器本地端執行。
* **白話比喻：** 單機遊戲——第一次下載大檔案，但進去後操作非常順暢。

### 3. Blazor Auto（`InteractiveAuto`）

* **運作原理：** 第一次進入網頁時用 Blazor Server（不用等下載），同時在背景下載 WASM 資源，下次開啟後自動切換成 WASM 模式。
* **白話比喻：** 先用雲端串流讓你立刻玩，等單機檔案下載好了再自動幫你切換。

### 三大模式快速對比

| 項目 | Blazor Server | Blazor WASM | Blazor Auto |
|---|---|---|---|
| 首頁載入速度 | **極快**（只需載入極小的 HTML/JS） | 較慢（需下載 .NET Runtime 與組件） | **極快**（初次由 Server 頂替） |
| 後續互動流暢度 | 視網路延遲而定 | **極快**（完全在本地端瀏覽器運算） | 初始一般，隨後極快 |
| 伺服器資源消耗 | **極高**（必須常駐連線並儲存所有 UI 狀態） | **極低**（只負責靜態檔案與 API） | 中等（初期耗資源，隨後釋放給用戶端） |
| 斷線容錯率 | 差（網路一斷即失去響應，跳出重連遮罩） | 好（支援離線運行） | 好（切換 WASM 後可離線） |
| 程式碼安全性 | **極高**（C# 商業邏輯完全鎖在伺服器） | 較低（DLL 會被下載到瀏覽器，可被反編譯） | 需注意（共享 UI 組件會被下載） |
| 架構複雜度 | **最簡單**（可直接呼叫 DB） | 中等（必須透過 Web API） | **最高**（需同時維護 Server 與 API 兩套邏輯） |

### 選型建議

#### 什麼時候選 Blazor Server？

* **企業內部系統（ERP、MIS、CRM）：** 內網速度快、延遲低，使用者人數受控。
* **需要絕對安全性：** 核心演算法或連線字串絕不能暴露給前端客戶端。
* **需要快速開發上線：** 不想另外拆寫 Web API，想在 UI 元件裡直接呼叫 Service。

#### 什麼時候選 Blazor WASM？

* **公開大眾網站：** 使用者人數極多，不希望伺服器因大量 WebSocket 連線而崩潰。
* **需要離線運行（PWA）：** 進出沒有網路的工廠、倉庫使用的盤點系統。
* **部署在靜態主機：** GitHub Pages、Azure Static Web Apps 或 CDN。

#### 什麼時候選 Blazor Auto？

* **重視「第一眼印象」的 SaaS 產品：** 既要進來就載入快，又要使用中流暢無延遲。
* **團隊能處理複雜架構：** Auto 模式要求元件在 Server 和 Client 兩端都能跑，必須用 DI 去抽換資料來源（Server 時直接讀 DB，WASM 時改呼叫 HttpClient）。

> **.NET 8+ 小建議：** 現在的 Blazor 不再是「整個專案只能選一種模式」。你可以預設使用 **Static SSR**（最省資源、SEO 最好），再對「需要頻繁互動的特定元件」單獨套用 `@rendermode InteractiveServer` 或 `InteractiveAuto`，彈性極大。

> **個人建議：** Blazor 最大的使用場景是 Blazor Server，其他模式用途不如前後端分離方案，以下說明都以 Blazor Server 為主。

---

## 三、SignalR 與 Circuit

### SignalR 是什麼

SignalR 是 Blazor Server 的底層通訊骨幹，在瀏覽器與伺服器之間建立一條**永遠不掛斷的電話線**，雙方隨時可以主動向對方發送資料。

* **底層技術：** 優先使用 WebSocket，若環境不支援則自動降級成 Server-Sent Events 或 Long Polling（長輪詢）。**開發者完全不需要手寫任何判斷邏輯。**

### SignalR 在 Blazor Server 的運作流程

1. **事件傳導：** 使用者點擊按鈕，瀏覽器把這個動作包裝成極小封包，透過 SignalR 瞬間傳給伺服器。
2. **畫面計算（DOM Diff）：** 伺服器執行 C# 商業邏輯，算出畫面改變前後的「最小差異藍圖」。
3. **局部渲染：** 伺服器把差異藍圖傳回瀏覽器，只針對改變的那一小塊 HTML 精準更新。

> **終極一句話：** SignalR 就是負責幫 Blazor Server 把前端使用者的「點擊動作」傳回伺服器大腦，再把大腦算好的「畫面差異」即時傳回前端渲染的隱形傳話筒。

### Circuit（電路）的概念

在 Blazor Server 中，每位使用者開啟一個瀏覽器分頁，就會建立一個 **Circuit**（電路）。Circuit 封裝了這位使用者的整個 SignalR 連線、元件狀態、以及所有 Scoped 服務。

| 事件 | 發生什麼 |
|---|---|
| 使用者開啟分頁 | Circuit 建立，Scoped 服務實例化 |
| 使用者在網站內切換頁面 | Circuit 持續存在，Scoped 服務保留狀態 |
| 使用者關閉分頁或按 F5 | Circuit 銷毀，Scoped 服務被清除 |
| 網路暫時中斷 | Circuit 進入緩衝期，等待重連 |

**與 Scoped 的核心連結（最重要的一環）：**

* 當大雄打開瀏覽器分頁，SignalR 連線建立，大雄專屬的 Scoped 服務（**專屬筆記本**）隨之被建立。
* 只要大雄不關閉這個分頁，不論他在網站內如何切換路由頁面，這條 SignalR 連線都持續活著，Scoped 服務就能完美記住他的登入資訊或購物車暫存。
* 一旦大雄關閉分頁，或按下 F5 重新整理，原本的 SignalR 連線被斷開銷毀，伺服器也會同步把大雄那本 Scoped 筆記本**丟進垃圾桶**。

### SignalR 逾時設定（`Program.cs`）

```csharp
builder.Services.AddServerSideBlazor(options =>
{
    // 斷線後保留 Circuit 狀態的最長時間（預設 3 分鐘）
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);

    // 詳細錯誤傳送給客戶端（只在開發環境開）
    options.DetailedErrors = builder.Environment.IsDevelopment();
});
```

### SignalR 訊息大小設定

```csharp
builder.Services.AddSignalR(options =>
{
    // 單次訊息最大大小（預設 32KB，這裡改成 10MB）
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});
```

### SignalR 的限制（必看）

| 限制 | 說明 |
|---|---|
| **伺服器記憶體殺手** | 每位使用者的 Circuit 常駐記憶體，千人同時在線壓力巨大 |
| **對網路延遲極度敏感** | Ping 值高的環境，每次點擊都有明顯黏滯感 |
| **不容許長時間斷線** | 斷線超過逾時時間，畫面跳出重連遮罩，使用者操作狀態全部消失 |

---

## 四、Razor 元件基礎語法

### 元件結構

一個 `.razor` 檔案就是一個元件，由三部分組成：

```razor
@* 1. 指令區（放在最頂端） *@
@page "/machine-status"
@inject IJSRuntime JSRuntime
@inject MachineService MachineService
@using System.ComponentModel.DataAnnotations

@* 2. HTML 標記區 *@
<h3>機台狀態看板</h3>
<p>機台 ID：@MachineId</p>
<button @onclick="Refresh">重新整理</button>

@* 3. C# 程式碼區 *@
@code {
    [Parameter]
    public string MachineId { get; set; } = "";

    private async Task Refresh()
    {
        await MachineService.RefreshAsync(MachineId);
    }
}
```

### 常用指令一覽

| 指令 | 用途 | 範例 |
|---|---|---|
| `@page` | 定義路由，讓這個元件可以被 URL 訪問 | `@page "/counter"` |
| `@inject` | 注入 DI 容器中的服務 | `@inject NavigationManager Nav` |
| `@using` | 引入命名空間（可以放 `_Imports.razor` 全域套用） | `@using MyApp.Services` |
| `@implements` | 宣告元件實作某個介面 | `@implements IDisposable` |
| `@inherits` | 讓元件繼承自某個基礎類別 | `@inherits ComponentBase` |
| `@layout` | 指定這個頁面使用哪個版型 | `@layout MainLayout` |
| `@attribute` | 加入 C# Attribute | `@attribute [Authorize]` |
| `@typeparam` | 定義泛型型別參數 | `@typeparam TItem` |
| `@code` | C# 程式碼區塊 | `@code { ... }` |

### 在 HTML 裡嵌入 C# 運算式

```razor
@* 單一變數 *@
<p>@MachineName</p>

@* 複雜運算式要加括號 *@
<p>@(IsRunning ? "運行中" : "已停止")</p>

@* 呼叫方法 *@
<p>@GetStatusText()</p>

@* 在屬性裡用 @ *@
<div class="@(IsError ? "alert alert-danger" : "")">內容</div>
```

### 條件渲染與迴圈

```razor
@* if/else *@
@if (IsLoading)
{
    <p>資料載入中...</p>
}
else if (machines.Count == 0)
{
    <p>目前沒有機台資料</p>
}
else
{
    <p>共 @machines.Count 台機台</p>
}

@* foreach 迴圈 *@
<ul>
@foreach (var machine in machines)
{
    <li>@machine.Name — @machine.Status</li>
}
</ul>

@* switch *@
@switch (machine.Status)
{
    case "Running":
        <span style="color:green">運行中</span>
        break;
    case "Error":
        <span style="color:red">異常</span>
        break;
    default:
        <span>停機</span>
        break;
}
```

### 渲染原始 HTML（`MarkupString`）

如果你的變數裡已經有 HTML 字串（例如從資料庫撈出的帶格式說明文字），直接 `@content` 會把 `<b>` 這種標籤當成純文字顯示。要渲染 HTML 需要明確轉型：

```razor
@((MarkupString)machineDescription)
```

> ⚠️ 只對你信任的內容使用，不要直接渲染使用者輸入的文字，否則有 XSS 風險。

---

## 五、元件生命週期

### 執行順序

```text
元件建立
  ↓
SetParametersAsync（接收並設定所有 [Parameter] 值）
  ↓
OnInitialized / OnInitializedAsync  ← 只執行一次，撈初始資料
  ↓
OnParametersSet / OnParametersSetAsync  ← 初始化後立即執行；之後每次 [Parameter] 變動再執行
  ↓
渲染 HTML 到瀏覽器
  ↓
OnAfterRender / OnAfterRenderAsync  ← 每次渲染完後執行（搞定 JS）
  ↓
... 使用者互動 → 重新渲染 → 再次觸發 OnParametersSet 和 OnAfterRender ...
  ↓
元件銷毀 → Dispose / DisposeAsync  ← 解除訂閱、釋放記憶體
```

### 各階段說明

#### `OnInitializedAsync` — 初始化，只跑一次

```csharp
protected override async Task OnInitializedAsync()
{
    // 適合：撈取頁面的初始資料
    machineList = await MachineService.GetAllAsync();

    // 訂閱 Singleton 服務的狀態事件
    DashboardState.OnStateChanged += HandleStateChanged;
}
```

#### `OnParametersSetAsync` — 每次參數改變都跑

```csharp
protected override async Task OnParametersSetAsync()
{
    // 適合：根據父元件傳進來的新 [Parameter] 重新撈資料
    // 例如：路由從 /machine/CNC-01 變成 /machine/CNC-02
    if (MachineId != _lastMachineId)
    {
        _lastMachineId = MachineId;
        machineDetail = await MachineService.GetDetailAsync(MachineId);
    }
}
```

> **注意：** 如果把資料初始化放在 `OnInitializedAsync` 而不是 `OnParametersSetAsync`，當路由參數在相同元件內改變時（例如 `/emp/1` → `/emp/2`），不會重新載入資料，因為元件實例已存在，`OnInitialized` 不會再觸發。

#### `OnAfterRenderAsync` — HTML 已在瀏覽器，可以動 JS

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // 只有第一次渲染完才初始化 JS 圖表庫
        // 在此之前 DOM 根本還不存在，直接呼叫 JS 會崩潰
        await JSRuntime.InvokeVoidAsync("initMachineChart", "canvas-id");
    }
}
```

#### `SetParametersAsync` — 最底層，通常不需要覆寫

只有需要攔截「參數設定」這個動作本身才覆寫，一般情況下不要動它：

```csharp
public override async Task SetParametersAsync(ParameterView parameters)
{
    // 如果覆寫，務必呼叫 base，否則生命週期後面的方法都不會執行
    await base.SetParametersAsync(parameters);
}
```

#### `ShouldRender` — 控制是否需要重新渲染

```csharp
protected override bool ShouldRender()
{
    // 回傳 false 可以跳過這次渲染，用於高頻率狀態更新的效能優化
    return _shouldRender;
}
```

#### `Dispose` — 清理，防止記憶體洩漏

```razor
@implements IDisposable

@code {
    public void Dispose()
    {
        // 取消所有事件訂閱，否則元件銷毀後服務還會繼續通知它，造成記憶體洩漏
        DashboardState.OnStateChanged -= HandleStateChanged;
    }
}
```

如果清理工作是非同步的：

```razor
@implements IAsyncDisposable

@code {
    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
            await _jsModule.DisposeAsync();
    }
}
```

### 完整骨架範例

```razor
@page "/lifecycle-demo/{MachineId}"
@implements IDisposable
@inject IJSRuntime JSRuntime
@inject MachineService MachineService
@inject MachineDashboardState DashboardState

<h3>機台：@MachineId</h3>
<p>狀態：@_status</p>

@code {
    [Parameter] public string MachineId { get; set; } = "";

    private string _lastMachineId = "";
    private string _status = "";

    // 1. 初始化：只跑一次，撈基礎資料、訂閱事件
    protected override async Task OnInitializedAsync()
    {
        DashboardState.OnStateChanged += HandleStateChanged;
        _status = await MachineService.GetStatusAsync(MachineId);
        _lastMachineId = MachineId;
    }

    // 2. 參數變更：路由 MachineId 改變時重新撈資料
    protected override async Task OnParametersSetAsync()
    {
        if (MachineId != _lastMachineId)
        {
            _lastMachineId = MachineId;
            _status = await MachineService.GetStatusAsync(MachineId);
        }
    }

    // 3. 渲染後：DOM 已存在，可以安全呼叫 JS
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JSRuntime.InvokeVoidAsync("initChart", "machine-chart");
    }

    // 4. 銷毀：乾淨地解除訂閱
    public void Dispose()
    {
        DashboardState.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);
}
```

### 開發小抄

| 問題 | 答案 |
|---|---|
| 要撈資料庫？ | 寫在 `OnInitializedAsync` |
| 按鈕點了沒反應、畫面沒變？ | 確認是不是漏了 `InvokeAsync(StateHasChanged)` |
| 有用 `+=` 訂閱 State？ | 確認有 `@implements IDisposable` 並在 `Dispose` 裡寫 `-=` |

---

## 六、StateHasChanged 與重新渲染

### 什麼時候 Blazor 自動重新渲染（不需要手動呼叫）

* Blazor 自己的事件處理器（`@onclick`、`@oninput` 等）觸發完成後
* `EventCallback` / `EventCallback<T>` 被呼叫完成後
* 父元件重新渲染導致 `[Parameter]` 有變化時

以上三種情況 Blazor 都會在事件結束後**自動觸發重新渲染**，不需要你手動呼叫 `StateHasChanged()`。

### 什麼時候必須手動呼叫

| 情境 | 正確寫法 |
|---|---|
| 從 Singleton/Scoped 服務的 `event Action` 回呼（非 Blazor 事件派發） | `InvokeAsync(StateHasChanged)` |
| 在 `async void` fire-and-forget 方法裡 | `await InvokeAsync(StateHasChanged)` |
| 在 `Task.Run(...)` 背景執行緒裡 | `await InvokeAsync(StateHasChanged)` |
| 在 Blazor 事件派發的同一同步執行緒裡（少數需要） | `StateHasChanged()` 直接呼叫即可 |

### 為什麼要用 `InvokeAsync`

Blazor Server 的渲染器有自己的同步脈絡（Synchronization Context）。**凡是從「非 Blazor 事件派發」的地方呼叫 `StateHasChanged`，都必須透過 `InvokeAsync` 把呼叫封送到正確的執行緒**，否則會丟出例外或產生難以追蹤的競態條件。

```razor
@implements IDisposable
@inject MachineDashboardState DashboardState

@code {
    protected override void OnInitialized()
    {
        // 從 Singleton 服務訂閱事件
        DashboardState.OnStateChanged += HandleStateChanged;
    }

    // 服務的事件是從不知道哪個執行緒呼叫的，必須用 InvokeAsync
    private void HandleStateChanged()
    {
        InvokeAsync(StateHasChanged); // 封送到渲染執行緒
    }

    public void Dispose()
    {
        // 務必取消訂閱，防止記憶體洩漏
        DashboardState.OnStateChanged -= HandleStateChanged;
    }
}
```

> **口訣：** 訂閱服務事件 → 一定用 `InvokeAsync(StateHasChanged)`；Blazor 的 `@onclick` 裡 → 不用加。

---

## 七、元件參數傳遞

### 1. 父 → 子：`[Parameter]`

```razor
@* 子元件 MachineBadge.razor *@
<div class="badge" style="background-color: @(IsRunning ? "green" : "red")">
    機台: @MachineName
</div>

@code {
    [Parameter] public string MachineName { get; set; } = "";
    [Parameter] public bool IsRunning { get; set; }
}
```

```razor
@* 父元件使用 *@
<MachineBadge MachineName="CNC-01" IsRunning="true" />
```

### 2. 子 → 父：`EventCallback`

```razor
@* 子元件 ScanButton.razor *@
<button class="btn btn-primary" @onclick="TriggerScan">模擬掃描條碼</button>

@code {
    [Parameter] public EventCallback<string> OnBarcodeScanned { get; set; }

    private async Task TriggerScan()
    {
        string mockBarcode = "WO-20260705-001";
        await OnBarcodeScanned.InvokeAsync(mockBarcode);
    }
}
```

```razor
@* 父元件 ProductionForm.razor *@
<p>目前接收到的工單: @_workOrder</p>
<ScanButton OnBarcodeScanned="HandleScan" />

@code {
    private string _workOrder = "";
    private void HandleScan(string barcode) => _workOrder = barcode;
}
```

### 3. 路由參數

```razor
@page "/machine/detail/{MachineId}"
@* 路由中的名稱必須與下方屬性名稱完全一致（大小寫不敏感）*@

<h3>機台明細看板</h3>
<p>當前查看的機台編號: @MachineId</p>

@code {
    [Parameter] public string MachineId { get; set; } = "";
}
```

```csharp
// 在 C# 邏輯中透過路徑拼接跳轉
@inject NavigationManager NavManager
NavManager.NavigateTo($"/machine/detail/{targetId}");
```

### 4. 跨多層：`[CascadingParameter]`

當元件巢狀很深（祖先 → 父親 → 兒子 → 孫子），不想一層一層用 `[Parameter]` 傳遞時使用：

```razor
@* 祖先元件（例如 MainLayout.razor 或頂層元件） *@
<CascadingValue Value="CurrentFactoryZone">
    @Body
</CascadingValue>

@code {
    private string CurrentFactoryZone = "A棟一樓製造區";
}
```

```razor
@* 孫子元件（任意深度） *@
<p>當前運行區域：@FactoryZone</p>

@code {
    // 只要型態對得上就能抓到，不需要管中間隔了幾層
    [CascadingParameter] public string FactoryZone { get; set; } = "";
}
```

### 5. 跨頁面業務狀態：Scoped 服務

如果你要在多個完全獨立的頁面（A 頁與 B 頁）之間傳遞複雜物件（如登入者的權限資訊），**不要用路由傳遞**，改用 `Scoped` 的 State 服務：

```csharp
// Services/UserSessionState.cs
public class UserSessionState
{
    public string EmpId { get; set; } = "";
    public string EmpName { get; set; } = "";

    // 可以另外實作事件通知 UI 刷新，此處僅示意儲存
}
```

```csharp
// Program.cs
builder.Services.AddScoped<UserSessionState>();
```

```razor
@* 頁面 A：寫入 *@
@inject UserSessionState Session
@code { void Login() { Session.EmpName = "張三"; } }
```

```razor
@* 頁面 B：讀取（同一個 Scoped 實例） *@
@inject UserSessionState Session
<p>歡迎登入，@Session.EmpName</p>
```

### 選型口訣

| 情境 | 方案 |
|---|---|
| 簡單的**上下層**關係 | `[Parameter]` / `EventCallback` |
| 跨分頁**查看特定資料明細** | 路由參數 |
| 畫面上所有元件都要讀取的**環境變數**（語系、主題） | `[CascadingParameter]` |
| 複雜的**跨頁面業務流程資料共享**（購物車、多步驟表單、登入資訊） | `Scoped` 服務 |

---

## 八、事件處理

### 基本寫法

```razor
@* 無參數 *@
<button @onclick="StartMachine">啟動</button>

@* 帶 EventArgs *@
<input @oninput="HandleInput" />
<button @onclick="HandleClick" />

@* Lambda（適合簡單邏輯） *@
<button @onclick="() => Count++">+1</button>

@* Lambda 帶參數 *@
<button @onclick="(e) => HandleClick(e, machineId)">明細</button>

@code {
    private void StartMachine() { /* ... */ }

    private void HandleInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
    }

    private void HandleClick(MouseEventArgs e)
    {
        Console.WriteLine($"座標：{e.ClientX}, {e.ClientY}");
    }

    private void HandleClick(MouseEventArgs e, string id) { /* ... */ }
}
```

### 非同步事件

```razor
<button @onclick="LoadData" disabled="@_isLoading">
    @(_isLoading ? "載入中..." : "載入資料")
</button>

@code {
    private bool _isLoading = false;

    private async Task LoadData()
    {
        _isLoading = true;
        await MachineService.RefreshAsync();
        _isLoading = false;
        // async Task 事件完成後 Blazor 自動 StateHasChanged，不需要手動呼叫
    }
}
```

### 常用事件類型

| 事件指令 | 對應 EventArgs | 常用屬性 |
|---|---|---|
| `@onclick` | `MouseEventArgs` | `ClientX/Y`、`Button`、`ShiftKey` |
| `@ondblclick` | `MouseEventArgs` | 同上 |
| `@onmousemove` | `MouseEventArgs` | 同上 |
| `@onkeydown` / `@onkeyup` | `KeyboardEventArgs` | `Key`、`Code`、`CtrlKey`、`AltKey` |
| `@oninput` | `ChangeEventArgs` | `Value`（字串） |
| `@onchange` | `ChangeEventArgs` | `Value`（字串） |
| `@onfocus` / `@onblur` | `FocusEventArgs` | — |
| `@onsubmit` | `EventArgs` | — |

### 阻止預設行為與事件冒泡

```razor
@* 阻止瀏覽器預設行為（例如阻止表單提交跳頁） *@
<form @onsubmit:preventDefault>
    <button type="submit">提交</button>
</form>

@* 阻止事件冒泡 *@
<div @onclick="ParentClick">
    父層
    <button @onclick="ChildClick" @onclick:stopPropagation>
        子層（不會觸發父層的 onclick）
    </button>
</div>
```

---

## 九、資料繫結（Data Binding）

### 單向繫結：顯示資料

```razor
<p>機台名稱：@MachineName</p>
```

### 雙向繫結：`@bind`

```razor
@* 等同於：value="@_name" + @onchange 自動更新 _name *@
<input @bind="_name" />
<p>你輸入：@_name</p>

@code { private string _name = ""; }
```

### 指定觸發時機：`@bind:event`

`@bind` 預設在 `onchange`（失去焦點）時更新。如果要即時（每個按鍵都更新）改用 `oninput`：

```razor
<input @bind="_searchText" @bind:event="oninput" placeholder="即時搜尋..." />
```

### 分離 get/set：`@bind:get` / `@bind:set`

適合在 set 時加入額外邏輯（例如資料驗證、觸發篩選）：

```razor
<input @bind:get="_searchText" @bind:set="OnSearchChanged" />

@code {
    private string _searchText = "";

    private async Task OnSearchChanged(string value)
    {
        _searchText = value;
        await FilterMachinesAsync(value); // set 時同時觸發篩選
    }
}
```

### 繫結子元件的 `Value`（雙向繫結簡寫）

對子元件使用 `@bind-Value` 是 `Value="..."` + `ValueChanged="..."` 的語法糖：

```razor
@* 父元件 *@
<MachineSelector @bind-Value="_selectedMachineId" />

@code { private string _selectedMachineId = ""; }
```

```razor
@* 子元件 MachineSelector.razor *@
<select @onchange="HandleChange">...</select>

@code {
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    private async Task HandleChange(ChangeEventArgs e)
    {
        await ValueChanged.InvokeAsync(e.Value?.ToString() ?? "");
    }
}
```

### `@bind:format` — 繫結格式化

```razor
@* 日期格式化：只顯示年月日 *@
<input type="date" @bind="_shiftDate" @bind:format="yyyy-MM-dd" />

@code { private DateTime _shiftDate = DateTime.Today; }
```

---

## 十、路由（Routing）

### 基本路由

```razor
@page "/machines"          @* 固定路由 *@
@page "/machines/{Id}"     @* 帶路由參數 *@
@page "/machines/{Id:int}" @* 帶型別限制的路由參數 *@
```

一個元件可以有多個 `@page`（對應多個 URL）：

```razor
@page "/report"
@page "/report/daily"
```

### 路由參數型別限制

```razor
@page "/machine/{MachineId:int}"
@page "/shift/{Date:datetime}"
@page "/batch/{Id:guid}"

@code {
    [Parameter] public int MachineId { get; set; }
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public Guid Id { get; set; }
}
```

| 限制 | 範例 | 說明 |
|---|---|---|
| `:int` | `{id:int}` | 整數 |
| `:long` | `{ticks:long}` | 長整數 |
| `:guid` | `{id:guid}` | GUID |
| `:bool` | `{active:bool}` | 布林（`true`/`false`） |
| `:datetime` | `{date:datetime}` | 日期時間 |
| `:decimal` | `{price:decimal}` | 小數 |
| `:nonfile` | `{param:nonfile}` | 非檔案路徑（避免攔截 `.css`/`.js`） |

### 選用路由參數

```razor
@page "/machine/{Id:int?}"  @* Id 可有可無 *@

@code {
    [Parameter] public int? Id { get; set; }

    protected override void OnParametersSet()
    {
        // Id 為 null 時顯示所有機台；有值時顯示特定機台
    }
}
```

### `NavigationManager` — 程式碼內跳轉

```razor
@inject NavigationManager NavManager

@code {
    private void GoToDetail(string machineId)
    {
        NavManager.NavigateTo($"/machine/{machineId}");
    }

    private void Reload()
    {
        // 強制重新整理整個頁面
        NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
    }

    private string CurrentUrl => NavManager.Uri;
}
```

### `NavLink` — 導覽連結（自動套用 active 樣式）

`NavLink` 比 `<a>` 好用，會在 URL 符合時自動加上 CSS class：

```razor
<NavLink href="/machines" Match="NavLinkMatch.All">
    機台列表
</NavLink>

<NavLink href="/report" Match="NavLinkMatch.Prefix">
    報表（符合 /report 開頭都算 active）
</NavLink>
```

| `Match` | 說明 |
|---|---|
| `NavLinkMatch.All` | URL 完全一致才 active（精確匹配） |
| `NavLinkMatch.Prefix` | URL 以此路徑開頭就 active（適合有子路由的頁面） |

### 查詢字串參數

```razor
@page "/search"
@inject NavigationManager NavManager

@code {
    private string _keyword = "";

    protected override void OnInitialized()
    {
        var uri = new Uri(NavManager.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers
            .ParseQuery(uri.Query);

        if (query.TryGetValue("keyword", out var kw))
            _keyword = kw!;
    }
}
```

---

## 十一、相依性注入（DI）

### 在 `Program.cs` 註冊服務

```csharp
// Singleton：整個 App 共用一個實例
builder.Services.AddSingleton<MachineDataCache>();

// Scoped：每個 SignalR Circuit（使用者分頁）一個實例
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddDbContext<AppDbContext>(); // DbContext 必須 Scoped

// Transient：每次注入都建立新實例
builder.Services.AddTransient<ExcelExportService>();
```

### 在元件裡注入

```razor
@inject MachineDataCache Cache        @* Singleton *@
@inject UserSessionState Session      @* Scoped *@
@inject ExcelExportService Exporter   @* Transient *@
@inject NavigationManager NavManager  @* 框架內建 *@
@inject IJSRuntime JSRuntime          @* 框架內建 *@
@inject ILogger<MyComponent> Logger   @* 框架內建 *@
```

### 框架內建的服務

Blazor Server 自動注冊，可以直接 `@inject`：

| 服務 | 用途 |
|---|---|
| `NavigationManager` | 取得目前 URL、程式碼跳轉 |
| `IJSRuntime` | 呼叫 JavaScript |
| `ILogger<T>` | 寫 Log |
| `IHttpContextAccessor` | 取得 HTTP Context（只在 Static SSR 可靠，互動元件中受限） |
| `AuthenticationStateProvider` | 取得目前使用者的登入狀態 |

---

## 十二、DI 生命週期詳解

這三種註冊方式都是「延遲載入（用到才啟動）」，核心差異在於物件建立後的**生命週期**與**共用範圍**。

### Singleton（單例）— 全廠共用的控制室主機

整個伺服器只有唯一一個實例。所有人拿到的都是同一個記憶體區塊。

* **優點：** 極度節省記憶體，適合集中管理不變的資料或全域資源。
* **缺點：** ⚠️ **絕對不能存使用者私人狀態！** 後進來的人會直接覆蓋前面的人的資料，造成嚴重的資安外洩。
* **適用：** 全廠靜態資料快取（部門清單、異常代碼表）、無狀態硬體管理工具。

### Scoped（範圍）— 每位員工當班的專屬筆記本

在 Blazor Server 中，它是綁定使用者的 **SignalR 連線（瀏覽器分頁）**。大雄跟胖虎的資料互不干擾，且能完美跨頁面記住當前使用者的狀態。

* **優點：** 安全隔離，能跨頁面保留狀態。
* **缺點：** 使用者開 100 個分頁就會有 100 個實例，記憶體消耗與連線人數成正比。
* **適用：** 使用者私人資訊（工號、姓名、部門）、購物車、工單暫存、`DbContext`。

### Transient（瞬時）— 隨寫隨丟的臨時便利貼

每次注入都給一個全新物件，用完就丟，絕不保留任何狀態。

* **優點：** 完全沒有線程安全問題，用完即丟。
* **缺點：** 在高頻迴圈裡大量建立會加重 GC 壓力。
* **適用：** 資料驗證器、計算公式、報表格式轉換工具。多執行緒 DB 操作時也可用 Transient `DbContext` 避免連線衝突。

### 快速選型口訣

> 1. **資料要全站快取、省記憶體，且大家唯讀** ➡️ `AddSingleton`
> 2. **資料跟「特定的人、特定分頁連線」綁定，要跨頁面記錄狀態** ➡️ `AddScoped`
> 3. **純工具人、用完就丟、不留任何記憶** ➡️ `AddTransient`

---

## 十三、Options Pattern（設定檔綁定）

### 為什麼要用 Options Pattern？

* **強型別優勢：** 避免散落在程式各處的 `Configuration["Key"]`，寫錯 key 會在編譯時期就報錯，並可直接利用 IDE 的 IntelliSense。
* **解耦：** 商業邏輯不需要知道設定檔長什麼樣子，只需要知道有一個 `MachineSettings` 物件。

### `appsettings.json`

```json
{
  "MachineSettings": {
    "MachineId": "CNC-001",
    "TimeoutSeconds": 30,
    "EnableLogging": true
  }
}
```

### 對應的 POCO 類別

```csharp
public class MachineSettings
{
    // 屬性名稱需對應 JSON 的 Key
    public string MachineId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public bool EnableLogging { get; set; }
}
```

### 在 `Program.cs` 註冊

```csharp
builder.Services.Configure<MachineSettings>(
    builder.Configuration.GetSection("MachineSettings"));
```

### 在元件裡使用

```razor
@inject IOptions<MachineSettings> MachineOptions

<p>機台編號：@MachineOptions.Value.MachineId</p>
<p>逾時設定：@MachineOptions.Value.TimeoutSeconds 秒</p>
```

### 三種注入介面差異

| 介面 | 特性 | 適用場景 |
|---|---|---|
| `IOptions<T>` | 啟動後不會更新 | 最常用，靜態設定值 |
| `IOptionsSnapshot<T>` | 每次請求重新讀取 | 需要不重啟就能生效的場景 |
| `IOptionsMonitor<T>` | 即時監聽，有 `OnChange` 事件 | 需要在設定變更時觸發邏輯 |

### 隱藏敏感資料

工廠環境中，請勿將 DB 連線字串或 MQTT 密碼直接寫在 `appsettings.json` 並推送到 Git：

* 開發期請使用：`dotnet user-secrets`
* 正式環境請使用：環境變數（Environment Variables）或 Azure Key Vault / AWS Parameter Store。

---

## 十四、JSInterop — C# 與 JavaScript 互動

> **Blazor Server 情境：** 以下範例基於互動式元件（InteractiveServer）。C# 與 JS 之間的呼叫都透過 SignalR 連線傳遞，因此必須在連線建立後才能使用（即 `OnAfterRenderAsync` 之後）。

Blazor Server 所有 UI 都在伺服器運算，但偶爾需要操作只存在瀏覽器的功能（如滾動位置、複製到剪貼板、呼叫第三方 JS 圖表庫）。這時就需要 `IJSRuntime` 跨越 C# 與 JS 的邊界。

### C# 呼叫 JavaScript

```razor
@inject IJSRuntime JSRuntime

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 無回傳值：初始化圖表或捲動到頂端
            await JSRuntime.InvokeVoidAsync("initMachineChart", "canvas-cnc01");

            // 有回傳值：取得瀏覽器視窗寬度
            int width = await JSRuntime.InvokeAsync<int>("getWindowWidth");
        }
    }
}
```

> ⚠️ **必須在 `OnAfterRenderAsync` 之後才呼叫 JS**，之前 DOM 還不存在。

### JS 模組隔離（推薦寫法）

把 JS 拆成小模組、用完就卸載，避免汙染全域 `window`：

```razor
@* MyPage.razor *@
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime

@code {
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 動態 import 對應的 JS 模組
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Pages/MyPage.razor.js");

            await _module.InvokeVoidAsync("initChart", "canvas-id");
        }
    }

    public async ValueTask DisposeAsync()
    {
        // 元件銷毀時一起釋放 JS 模組
        if (_module is not null)
            await _module.DisposeAsync();
    }
}
```

對應的 `MyPage.razor.js`：

```javascript
// 只 export 必要的函式，不汙染全域 window
export function initChart(canvasId) {
    // 初始化 Chart.js 或其他圖表庫
}
```

### JavaScript 呼叫 C#（`DotNetObjectReference`）

有時需要讓 JS 觸發 C# 方法，例如「掃描器 JS SDK 偵測到條碼後，通知 C# 元件」：

```razor
@implements IDisposable
@inject IJSRuntime JSRuntime

@code {
    private DotNetObjectReference<MachineMonitor>? _dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 把這個元件的 C# 實體傳給 JS
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("registerScannerCallback", _dotNetRef);
        }
    }

    // JS 透過 invokeMethodAsync 呼叫，方法必須是 public + [JSInvokable]
    [JSInvokable]
    public async Task OnBarcodeDetected(string barcode)
    {
        // 條碼掃描回調：更新畫面
        CurrentBarcode = barcode;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        // 一定要 Dispose，否則 JS 那邊的參考會讓 GC 無法回收這個元件
        _dotNetRef?.Dispose();
    }

    private string CurrentBarcode = "";
}
```

對應 JS：

```javascript
let _dotNetHelper;

export function registerScannerCallback(dotNetHelper) {
    _dotNetHelper = dotNetHelper;

    // 假設掃描器 SDK 有個 onScan 事件
    BarcodeScanner.onScan((barcode) => {
        _dotNetHelper.invokeMethodAsync('OnBarcodeDetected', barcode);
    });
}
```

### 重點整理

| 方向 | 工具 | 注意事項 |
|---|---|---|
| C# → JS（無回傳） | `InvokeVoidAsync("函式名", 參數...)` | 只能在 `OnAfterRenderAsync` 之後呼叫 |
| C# → JS（有回傳） | `InvokeAsync<T>("函式名", 參數...)` | 同上 |
| JS → C# | `DotNetObjectReference` + `[JSInvokable]` | 元件 `Dispose` 時必須釋放 `DotNetObjectReference` |

---

## 十五、ErrorBoundary — 元件層級錯誤捕捉

`ErrorBoundary` 是 Blazor 內建元件，可以包住某一段 UI 子樹，當裡面的元件在**渲染或生命週期方法**中丟出未處理例外時，攔截錯誤並顯示備用 UI，而不是讓整個頁面崩潰。

### 基本用法

```razor
<ErrorBoundary>
    <ChildContent>
        @* 這裡面發生例外，不會炸掉整個頁面 *@
        <MachineStatusPanel MachineId="CNC-01" />
    </ChildContent>
    <ErrorContent Context="ex">
        <MudAlert Severity="Severity.Error" Class="ma-4">
            機台狀態載入失敗：@ex.Message
        </MudAlert>
    </ErrorContent>
</ErrorBoundary>
```

### 讓使用者可以重試

```razor
<ErrorBoundary @ref="_errorBoundary">
    <ChildContent>
        <MachineStatusPanel MachineId="@MachineId" />
    </ChildContent>
    <ErrorContent>
        <MudStack Spacing="2" Class="ma-4">
            <MudText Color="Color.Error">資料載入失敗，請重試</MudText>
            <MudButton Variant="Variant.Outlined"
                       Color="Color.Primary"
                       OnClick="Recover">
                重新載入
            </MudButton>
        </MudStack>
    </ErrorContent>
</ErrorBoundary>

@code {
    [Parameter] public string MachineId { get; set; } = "";
    private ErrorBoundary? _errorBoundary;

    // 呼叫 Recover() 會清除錯誤狀態，重新嘗試渲染 ChildContent
    private void Recover() => _errorBoundary?.Recover();
}
```

### 自訂 ErrorBoundary（附 Log 功能）

```razor
@* Components/Shared/AppErrorBoundary.razor *@
@inherits ErrorBoundary
@inject ILogger<AppErrorBoundary> Logger

@code {
    // 覆寫 OnErrorAsync 可以在錯誤發生時自動寫 Log
    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "元件渲染發生未預期的錯誤");
        return base.OnErrorAsync(exception);
    }
}
```

使用方式跟內建的 `<ErrorBoundary>` 完全一樣，只需換標籤名稱：

```razor
<AppErrorBoundary>
    <ChildContent>
        <MachineStatusPanel MachineId="CNC-01" />
    </ChildContent>
    <ErrorContent Context="ex">
        <MudAlert Severity="Severity.Error">@ex.Message</MudAlert>
    </ErrorContent>
</AppErrorBoundary>
```

### 重要限制

| 能攔截 | 不能攔截 |
|---|---|
| 渲染（Render）過程中的例外 | `@onclick` 等事件處理器裡的例外 |
| 生命週期方法（`OnInitializedAsync` 等）的例外 | `async void` 逃逸的例外 |

> **實務建議：** 把「需要網路或 DB 呼叫」的子元件都用 `ErrorBoundary` 包起來，就能讓局部失敗不影響整頁其他功能正常運作。

---

## 十六、表單驗證（EditForm / DataAnnotations）

Blazor 內建的 `EditForm` 搭配 C# DataAnnotations 可以在**不寫一行 JavaScript** 的情況下，做到即時的表單欄位驗證。

### Model 加資料標注

```csharp
// Models/ProductFormModel.cs
using System.ComponentModel.DataAnnotations;

public class ProductFormModel
{
    [Required(ErrorMessage = "商品名稱不能為空")]
    [MaxLength(100, ErrorMessage = "商品名稱最多 100 字")]
    public string Name { get; set; } = "";

    [Range(0, 9999999, ErrorMessage = "價格必須為 0 以上的數字")]
    public decimal Price { get; set; }

    [Url(ErrorMessage = "請輸入有效的網址格式（例如 https://...）")]
    public string? ProductUrl { get; set; }
}
```

### 表單元件（搭配 MudBlazor）

```razor
@* AddProductDialog.razor *@
@inject MenuService MenuService

<EditForm Model="_model" OnValidSubmit="HandleSubmit">
    @* DataAnnotationsValidator 是啟動 DataAnnotations 驗證的開關，缺它不動 *@
    <DataAnnotationsValidator />

    <MudStack Spacing="3">
        @* For="..." 讓 MudBlazor 自動把驗證訊息顯示在欄位下方，不需要另外加 <ValidationMessage> *@
        <MudTextField @bind-Value="_model.Name"
                      For="@(() => _model.Name)"
                      Label="商品名稱"
                      Immediate="true" />   @* Immediate="true"：每次輸入都立即觸發驗證 *@

        <MudTextField @bind-Value="_model.Price"
                      For="@(() => _model.Price)"
                      Label="售價"
                      Adornment="Adornment.Start"
                      AdornmentText="$" />

        <MudTextField @bind-Value="_model.ProductUrl"
                      For="@(() => _model.ProductUrl)"
                      Label="商品連結（選填）" />

        @* 只有通過所有驗證，才會觸發 OnValidSubmit *@
        <MudButton ButtonType="ButtonType.Submit"
                   Variant="Variant.Filled"
                   Color="Color.Primary">
            新增商品
        </MudButton>
    </MudStack>
</EditForm>

@code {
    private ProductFormModel _model = new();

    private async Task HandleSubmit()
    {
        // 進到這裡代表所有 DataAnnotations 都通過了
        await MenuService.AddProductAsync(_model);
        _model = new(); // 清空表單
    }
}
```

### `OnValidSubmit` vs `OnSubmit`

| 事件 | 觸發時機 | 使用時機 |
|---|---|---|
| `OnValidSubmit` | 驗證全部通過才觸發 | 99% 的情況用這個 |
| `OnSubmit` | 不論驗證結果都觸發 | 需要自行決定是否繼續時 |

若選 `OnSubmit`，需手動檢查：

```csharp
private async Task HandleSubmit(EditContext context)
{
    if (!context.Validate()) return;
    // 手動驗證通過後的邏輯
}
```

### 常用 DataAnnotations 一覽

| 標注 | 說明 |
|---|---|
| `[Required]` | 不能為空（字串不能是空白） |
| `[MaxLength(n)]` | 字串最大長度 |
| `[MinLength(n)]` | 字串最小長度 |
| `[Range(min, max)]` | 數值範圍 |
| `[EmailAddress]` | 電子郵件格式 |
| `[Url]` | URL 格式 |
| `[RegularExpression(pattern)]` | 自訂正規表達式 |
| `[Compare("OtherProp")]` | 兩欄位值必須相同（常用在確認密碼） |

---

## 十七、登入登出與 Cookie 驗證

### 為什麼不能在 Blazor 元件裡直接呼叫 `SignInAsync`

Blazor Server 的運作方式是：瀏覽器先用一般 HTTP 要求把頁面載下來，接著建立一條 SignalR 連線（WebSocket 或長輪詢），之後所有的互動（按鈕點擊、資料更新）都是透過這條已開啟的連線傳遞，而**不是新的 HTTP 請求**。

問題在於：`HttpContext.SignInAsync()` 的本質是在 HTTP Response 裡寫入 `Set-Cookie` 標頭。但 SignalR 連線建立之後，那個初始的 HTTP Response 早就已經送出、結束了，你沒有辦法再回頭修改一個已經送出的回應的標頭。

因此：**登入、登出這種需要寫入 Cookie 的操作，必須發生在一次獨立、正常的 HTTP 請求生命週期中**，也就是一次傳統的表單 POST。

### 為什麼是 Cookie

ASP.NET Core 的 Cookie Authentication 運作方式：

1. 使用者登入成功後，伺服器把使用者身分（`ClaimsPrincipal`）加密序列化，透過 `Set-Cookie` 存到瀏覽器。
2. 之後瀏覽器每次發出請求（**包含 Blazor 用來建立 SignalR 連線的那次 HTTP 協商請求**），都會自動夾帶這個 Cookie。
3. 伺服器的 Authentication Middleware 讀取這個 Cookie，還原出使用者身分，Blazor 的 `AuthenticationStateProvider` 才能拿到正確的登入狀態。

Cookie 是讓 SignalR 這條長連線「知道你是誰」的橋樑，沒有它，Blazor Server 每次重新整理或重新連線都會變成匿名使用者。

### AntiforgeryToken 是什麼（CSRF 防護）

因為 Cookie 會被瀏覽器自動夾帶，如果你的登入或登出端點只認 Cookie、不做其他驗證，那惡意網站可以放一個隱藏表單，誘導已登入使用者的瀏覽器對你的網站發出偽造的 POST，而使用者的瀏覽器會乖乖把 Cookie 一起送過去，伺服器單看 Cookie 會誤以為這是本人操作（**這就是 CSRF 攻擊**）。

ASP.NET Core 的 Antiforgery 系統會：

1. 產生一組 token，一份放進表單的隱藏欄位，一份放進另一個 Cookie。
2. 表單送出時，伺服器同時檢查兩個 token 是否匹配。
3. 因為第三方網站沒辦法讀到你網站的 Cookie 值去填進它偽造的表單裡，所以能有效擋掉 CSRF。

### `Program.cs` 設定

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 註冊 Cookie 驗證機制
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// 註冊防偽造 token 服務
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseAuthentication();
app.UseAntiforgery();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### 登入登出端點（Minimal API）

```csharp
// 這兩個端點是一般的 HTTP endpoint，不是 Blazor 元件，因此可以正常寫入 Set-Cookie

app.MapPost("/login", async (HttpContext httpContext, LoginModel model) =>
{
    // 實際驗證帳號密碼（此處省略）
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, model.Username)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    // 關鍵：這一行會在 Response 寫入 Set-Cookie
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.Redirect("/");
});

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

public class LoginModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### 登入頁面（靜態 SSR，走真正的 HTTP POST）

```razor
@page "/login"
@* 注意：這個頁面刻意不加 @rendermode，維持靜態 SSR，讓表單走真正的 HTTP POST *@
@* 如果加了 InteractiveServer，<form> 會走 SignalR，Set-Cookie 就會失效 *@

<h3>登入</h3>

<form method="post" action="/login">
    @* AntiforgeryToken 元件會自動輸出隱藏欄位與對應的驗證資訊 *@
    <AntiforgeryToken />

    <div>
        <label>帳號</label>
        <input type="text" name="Username" />
    </div>
    <div>
        <label>密碼</label>
        <input type="password" name="Password" />
    </div>
    <button type="submit">登入</button>
</form>
```

### 登出按鈕（同樣走靜態表單 POST）

```razor
@* 可以放在 NavMenu 或任何靜態渲染的區塊 *@
<form method="post" action="/logout">
    <AntiforgeryToken />
    <button type="submit">登出</button>
</form>
```

### 重點整理

| 元素 | 作用 |
|---|---|
| HTTP POST | 讓伺服器有機會在這次獨立的 HTTP Response 裡寫入 `Set-Cookie`，SignalR 連線內做不到這件事 |
| Cookie | 儲存已加密的使用者身分，讓後續每次請求（包含 SignalR 協商）都能還原登入狀態 |
| AntiforgeryToken | 驗證這次 POST 確實是從你自己網站的表單發出，防止 CSRF 偽造請求 |

> **簡單一句話：** 登入登出本質上是「修改身分狀態」這種有副作用的操作，必須回到傳統 HTTP 請求 / 回應模型裡完成，Blazor Server 的即時連線只負責畫面互動，不負責也不能負責這件事。

---

## 十八、多語系（Localization）

### 為什麼用 `L["Key"]` 這種字串索引寫法

ASP.NET Core 的多語系機制核心是 `IStringLocalizer<T>`，它的設計理念：

* **資源檔與程式碼分離：** 文字內容放在 `.resx` 資源檔，翻譯人員只需改資源檔，不需重新編譯程式碼。
* **Key 查表機制：** `L["Login.Title"]` 拿字串當 key，去查目前 `CultureInfo` 對應的資源檔。
* **容錯與可追蹤性：** 如果某個 key 在對應語言的資源檔裡沒有翻譯，`IStringLocalizer` 預設直接回傳 key 本身，方便開發與 QA 一眼看出漏翻譯的項目。
* **支援格式化參數：** `L["WelcomeUser", userName]` 可以像 `string.Format` 一樣帶入參數，資源檔內容寫成 `歡迎, {0}!`。

### 為什麼切換語言要整頁重新整理

Blazor Server 的 SignalR 連線建立時，`RequestLocalizationMiddleware` 只判斷**一次**語系，並且固定在整個 Circuit 存續期間。在互動元件裡改 `CultureInfo` 只是暫時的，重新整理後就會被洗回去。

所以：**語言設定要寫進 Cookie，並強制整頁重新整理，讓下一次請求重新走過 Middleware Pipeline。**

### `Program.cs` 設定

```csharp
// 註冊 Localization 服務，指定 resx 資源檔放置路徑
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[]
{
    new CultureInfo("zh-TW"),
    new CultureInfo("en-US")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("zh-TW");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Provider 判斷順序：QueryString → Cookie → Accept-Language
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// 必須放在其他 Middleware 之前，確保後續流程都拿得到正確的 CultureInfo
app.UseRequestLocalization();
```

### 資源檔結構

```
/Resources
    SharedResource.zh-TW.resx      (Key: Account.Login.Title, Value: 登入)
    SharedResource.en-US.resx      (Key: Account.Login.Title, Value: Login)

public class SharedResource { }   // 空類別，僅作為資源檔的標記類型
```

### 在元件裡使用

```razor
@page "/login"
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResource> L

<h3>@L["Account.Login.Title"]</h3>

<form method="post" action="/login">
    <AntiforgeryToken />
    <label>@L["Account.Login.UsernameLabel"]</label>
    <input type="text" name="Username" />
    <button type="submit">@L["Account.Login.ButtonSubmit"]</button>
</form>
```

### 切換語言的 Endpoint

```csharp
app.MapGet("/set-culture", (string culture, string redirectUri, HttpContext httpContext) =>
{
    // 將使用者選擇的語系寫入 Cookie，有效期設定 1 年
    httpContext.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true
        });

    return Results.Redirect(redirectUri);
});
```

```razor
@* 用一般連結，讓瀏覽器發出全新的 HTTP 請求，重新走過 RequestLocalizationMiddleware *@
<a href="/set-culture?culture=zh-TW&redirectUri=/">中文</a>
<a href="/set-culture?culture=en-US&redirectUri=/">English</a>
```

### Key 命名建議（結構化）

採用「**模組.功能.用途**」的階層命名方式，使用 `PascalCase`，以 `.` 分隔階層：

| 原則 | 說明 |
|---|---|
| 結構化 Key | `Account.Login.Title` 比 `L["登入"]` 穩定，原文修改不影響 key |
| 共用文字歸 Common | `Common.Validation.Required`、`Common.Error.NotFound` 避免重複定義 |
| Key 只是識別碼 | 實際文字永遠放在 `.resx` 裡，不要把完整句子塞進 key |
| 避免單一巨大檔案 | 同一支資源檔對應同一個類別或功能模組 |

```
Account.Login.Title
Account.Login.ButtonSubmit
Account.Login.InvalidCredentials
Common.Validation.Required
Common.Validation.MaxLength
Common.Error.NotFound
```

### 重點整理

| 元素 | 作用 |
|---|---|
| `IStringLocalizer<T>` + `L["Key"]` | 讓文字與程式碼分離，透過 key 查表取得對應語系的文字，缺翻譯時容易被發現 |
| 結構化 Key 命名 | 避免原文修改牽連所有呼叫端，維持長期可維護性 |
| Cookie | 讓語言偏好能跨頁面、跨 SignalR 連線持久保存 |
| 整頁重新導向 | 語言切換必須透過真正的 HTTP 請求才能生效，跟登入登出的道理相同 |

---

## 十九、快速選型備忘錄

### 我要在兩個元件之間傳資料

| 情境 | 方案 |
|---|---|
| 父傳給子 | `[Parameter]` |
| 子通知父 | `EventCallback` |
| 跨多層（祖→孫） | `[CascadingParameter]` |
| 跨頁面（A頁→B頁查明細） | 路由參數 |
| 跨頁面（複雜業務狀態） | `Scoped` 服務 |

### 我需要執行某件事

| 需求 | 在哪裡做 |
|---|---|
| 頁面載入時撈初始資料 | `OnInitializedAsync` |
| 路由參數改變時重新撈資料 | `OnParametersSetAsync` |
| 初始化 JS 套件（圖表、第三方 UI） | `OnAfterRenderAsync(firstRender)` |
| 呼叫 JavaScript | `IJSRuntime.InvokeVoidAsync` / `InvokeAsync<T>` |
| 服務事件更新 UI | 訂閱事件 + `InvokeAsync(StateHasChanged)` |
| 清理事件訂閱 | `Dispose` / `DisposeAsync` |

### 我不確定用哪個 DI 生命週期

| 需求特徵 | 選擇 |
|---|---|
| 全站共用、大家唯讀、省記憶體 | `Singleton` |
| 跟特定使用者/分頁綁定，要跨頁記憶 | `Scoped` |
| 純工具，用完即丟，不留狀態 | `Transient` |

### 常見地雷提醒

| 地雷 | 解法 |
|---|---|
| 按鈕點了沒反應 | 確認 `async Task` 沒有寫成 `async void` |
| 畫面不更新 | 服務事件回呼要用 `InvokeAsync(StateHasChanged)` |
| 記憶體持續增長 | `+=` 訂閱的事件要在 `Dispose` 裡 `-=` |
| JS 呼叫出錯 | 確認是在 `OnAfterRenderAsync` 之後，不是 `OnInitializedAsync` |
| 登入後 Blazor 還是匿名 | 確認登入頁沒有加 `@rendermode InteractiveServer` |
| 切換語言沒效 | 確認有強制整頁重新整理，不是只改 `CultureInfo` |
| Virtualize 不出現 | 外層容器必須設定固定高度 + `overflow-y: auto` |
| DynamicComponent 參數沒套用 | 字典 key 必須與 `[Parameter]` 屬性名稱完全相同（大小寫一致） |
| ShouldRender 回傳 false 後畫面永遠不更新 | 要在資料變更時先設回 `true` 再呼叫 `StateHasChanged()` |

---

## 二十、ShouldRender — 元件重繪控制

> **Blazor Server 效能優化重點：** 每次 SignalR 事件觸發後，伺服器需要對整棵元件樹做 DOM diff 計算。在高頻狀態更新（如機台監視面板的計時器）中，`ShouldRender()` 可以讓沒有實際改變的子元件直接略過，顯著降低伺服器運算和 SignalR 傳輸量。

### 什麼是 ShouldRender

每次父元件重繪、或呼叫 `StateHasChanged()` 時，Blazor 預設都會讓子元件也跟著重新評估渲染樹。`ShouldRender()` 讓你**告訴 Blazor：這次不需要重繪我**，直接略過，省下 diff 和 DOM 操作的成本。

### 基本用法

```razor
@code {
    private bool _needsRender = true;

    protected override bool ShouldRender()
    {
        // 只有 _needsRender 為 true 時才允許重繪
        if (!_needsRender) return false;
        _needsRender = false;  // 重繪一次後重置為 false
        return true;
    }

    // 當真正有新資料時，才打開重繪開關
    private void RefreshView()
    {
        _needsRender = true;
        StateHasChanged();
    }
}
```

### 實用模式：參數沒變就不重繪

```razor
@code {
    [Parameter] public string? MachineStatus { get; set; }

    private string? _lastRendered;

    protected override bool ShouldRender()
    {
        if (MachineStatus == _lastRendered) return false;
        _lastRendered = MachineStatus;
        return true;
    }
}
```

**適用場景：** 機台狀態列表中有數十個 `MachineStatusCard` 元件，但每次計時器觸發只有少數幾台狀態改變，用 `ShouldRender` 讓沒改變的卡片直接略過重繪。

### 注意事項

| 項目 | 說明 |
|---|---|
| 首次渲染 | `ShouldRender()` 不會被呼叫——元件一定會渲染一次 |
| 回傳 `false` 後 | `OnAfterRender` 仍會被呼叫（`firstRender: false`） |
| 別過度使用 | 只用在確定需要效能優化的高頻元件，不是所有元件都需要 |
| 配合 `StateHasChanged` | 要重繪時，先設回旗標為 `true` 再呼叫 |

---

## 二十一、`<Virtualize>` 長清單虛擬化

> **Blazor Server 常見模式：** 在多使用者環境中，每個 Circuit 各自維護 UI 狀態。把幾百筆 DOM 全部渲染出來，會讓每次 SignalR diff 的計算量倍增。用 `<Virtualize>` 只渲染可見區域，可以同時改善使用者體感速度和伺服器負載。

### 什麼是 Virtualize

當清單有幾百、幾千筆資料時，把所有 DOM 元素都渲染出來會讓頁面很卡。`<Virtualize>` 只渲染**目前可見視窗內的項目**（加上少量緩衝），滾動時動態補充，大幅降低 DOM 節點數量。

**白話比喻：** 捷運站的電子看板——只顯示你現在站位前後幾班車，不是把今天所有班次都列出來。

### 基本用法（完整 List）

```razor
@* 外層容器必須有固定高度 + overflow-y: auto *@
<div style="height: 500px; overflow-y: auto;">
    <Virtualize Items="@machines" Context="m" OverscanCount="5">
        <ItemContent>
            <div class="machine-row">
                <span>@m.Id</span>
                <span>@m.Name</span>
                <span>@m.Status</span>
            </div>
        </ItemContent>
        <Placeholder>
            <div class="skeleton-row">載入中...</div>
        </Placeholder>
    </Virtualize>
</div>

@code {
    private List<Machine> machines = new();

    protected override async Task OnInitializedAsync()
    {
        machines = await MachineService.GetAllAsync();
    }
}
```

### 進階用法（`ItemsProvider`，分頁非同步載入）

適合資料量超大（幾萬筆），不想一次全撈進記憶體的場景：

```razor
<Virtualize ItemsProvider="@LoadMachines" Context="m" ItemSize="60">
    <ItemContent>
        <div style="height: 60px;">@m.Name — @m.Status</div>
    </ItemContent>
</Virtualize>

@code {
    private async ValueTask<ItemsProviderResult<Machine>> LoadMachines(
        ItemsProviderRequest request)
    {
        // Blazor 會告訴你目前視窗需要從第幾筆開始、要幾筆
        var result = await MachineService.GetPageAsync(
            skip: request.StartIndex,
            take: request.Count,
            cancellationToken: request.CancellationToken);

        return new ItemsProviderResult<Machine>(result.Items, result.TotalCount);
    }
}
```

### 參數速查

| 參數 | 型別 | 說明 |
|---|---|---|
| `Items` | `ICollection<T>` | 完整的記憶體清單 |
| `ItemsProvider` | `ItemsProviderDelegate<T>` | 非同步分頁載入（二擇一） |
| `ItemSize` | `float` | 每個項目的估計高度（px），用 `ItemsProvider` 時必填 |
| `OverscanCount` | `int` | 視窗外預先渲染的額外項目數（預設 3） |
| `Context` | `string` | 迴圈變數名稱（預設 `context`） |

### 重要限制

* 外層容器**一定要設定固定高度** + `overflow-y: auto`，否則 Virtualize 無法計算視窗範圍
* 所有項目最好有**一致的高度**，否則虛擬化計算會不準
* 若與 MudBlazor 的 `MudTable` 一起用，通常 MudTable 自己已有分頁，不需要再套 Virtualize

---

## 二十二、QuickGrid 資料表格

> **Blazor Server 最佳實踐：** QuickGrid 搭配 `IQueryable<T>` + EF Core，排序和分頁的 SQL 直接推送到資料庫，避免把全部資料撈進每個 Scoped DbContext 的記憶體。在多使用者同時在線的 Blazor Server 環境中，這對記憶體和查詢效能的影響非常顯著。

### 什麼是 QuickGrid

.NET 8 正式引入的官方高效能資料表格元件，內建排序、分頁，並可直接接 `IQueryable<T>` 把排序/分頁的 SQL 推送到資料庫（而非撈全部再過濾）。

> 若已使用 MudBlazor 的 `MudTable`，兩者功能重疊，**二擇一即可**。

### 安裝

```xml
<!-- .csproj 加入（.NET 9+ 已內建在 Microsoft.AspNetCore.Components.Web） -->
<PackageReference Include="Microsoft.AspNetCore.Components.QuickGrid" Version="10.*" />
```

```razor
@* _Imports.razor 或個別頁面加入 *@
@using Microsoft.AspNetCore.Components.QuickGrid
```

### 基本用法

```razor
@page "/machine-grid"

<QuickGrid Items="@FilteredMachines" Pagination="@pagination">
    <PropertyColumn Property="@(m => m.Id)"     Title="機台編號" Sortable="true" />
    <PropertyColumn Property="@(m => m.Name)"   Title="機台名稱" Sortable="true" />
    <PropertyColumn Property="@(m => m.Zone)"   Title="廠區" />
    <PropertyColumn Property="@(m => m.Status)" Title="狀態" />
    <TemplateColumn Title="操作">
        <button @onclick="@(() => ViewDetail(context))">查看</button>
    </TemplateColumn>
</QuickGrid>

<Paginator State="@pagination" />

@code {
    [Inject] IMachineService MachineService { get; set; } = default!;

    private IQueryable<Machine>? allMachines;
    private PaginationState pagination = new() { ItemsPerPage = 20 };

    private IQueryable<Machine>? FilteredMachines =>
        allMachines?.Where(m => m.IsActive);

    protected override async Task OnInitializedAsync()
    {
        // AsQueryable() 讓 QuickGrid 可以把排序/分頁推送給 EF Core
        var list = await MachineService.GetAllAsync();
        allMachines = list.AsQueryable();
    }

    void ViewDetail(Machine m) { /* 導頁到詳細頁 */ }
}
```

### 欄位型別

| 欄位元件 | 說明 |
|---|---|
| `<PropertyColumn>` | 綁定 Lambda 表達式，支援 `Sortable`、`Format`、`Title` |
| `<TemplateColumn>` | 自訂渲染，用 `context` 存取當列資料 |

### 關鍵參數

```razor
<QuickGrid Items="@data"
           Pagination="@pagination"
           RowClass="@(m => m.HasAlert ? "alert-row" : null)"  @* 動態列樣式 *@
           Theme="default">
```

### EF Core 整合（效能最大化）

```razor
@* 直接傳 DbContext 的 IQueryable，排序和分頁的 SQL 由 EF Core 生成 *@
<QuickGrid Items="@DbContext.Machines.Where(m => m.ZoneId == currentZone)" />
```

---

## 二十三、DynamicComponent 動態元件

### 什麼是 DynamicComponent

在執行時期根據 `Type` 決定要渲染哪個元件，不需要在樣板裡用 `@if`/`@switch` 一個個列出。適合做**可設定化的儀表板、動態 Tab 系統、插件化 UI**。

### 基本用法

```razor
<DynamicComponent Type="@currentPanel" Parameters="@panelParams" />

<div>
    <button @onclick="ShowMachineStatus">機台狀態</button>
    <button @onclick="ShowProductionChart">產量圖表</button>
</div>

@code {
    private Type currentPanel = typeof(MachineStatusPanel);

    private Dictionary<string, object> panelParams = new()
    {
        { "ZoneId", 1 },
        { "ShowDetails", true }
    };

    void ShowMachineStatus()
    {
        currentPanel = typeof(MachineStatusPanel);
        panelParams = new() { { "ZoneId", 1 }, { "ShowDetails", true } };
    }

    void ShowProductionChart()
    {
        currentPanel = typeof(ProductionChartPanel);
        panelParams = new() { { "DateRange", "today" } };
    }
}
```

### 用 `@ref` 呼叫動態元件的方法

```razor
<DynamicComponent Type="@currentPanel"
                  Parameters="@panelParams"
                  @ref="dynamicRef" />

@code {
    private DynamicComponent? dynamicRef;

    void Refresh()
    {
        // 強制轉型到已知介面，再呼叫方法
        if (dynamicRef?.Instance is IRefreshablePanel panel)
        {
            panel.Refresh();
        }
    }
}
```

### 實用模式：Tab 系統

```razor
@* 用字典定義 Tab → 元件 Type 對應關係 *@
@code {
    private static readonly Dictionary<string, Type> _tabs = new()
    {
        { "status",     typeof(MachineStatusPanel) },
        { "production", typeof(ProductionPanel) },
        { "alerts",     typeof(AlertPanel) },
    };

    private string _activeTab = "status";
    private Type ActivePanel => _tabs[_activeTab];
}

<div class="tab-bar">
    @foreach (var tab in _tabs)
    {
        <button @onclick="@(() => _activeTab = tab.Key)">@tab.Key</button>
    }
</div>

<DynamicComponent Type="@ActivePanel" />
```

### 注意事項

| 項目 | 說明 |
|---|---|
| 參數字典的 key | 必須與 `[Parameter]` 屬性名稱**完全相同**（大小寫一致） |
| 多餘的 key | 靜默忽略，不會報錯 |
| `Type` 為 `null` | 什麼都不渲染 |
| 適用模式 | 所有 render mode 均可，MAUI Hybrid 也適用 |

