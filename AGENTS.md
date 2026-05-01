# AGENTS.md — PCCustomizer Project Agent Guide

> Last updated: 2026-05-01
> Audience: All AI agents operating in this project (Claude Code, etc.)

---

## 1. Project Overview

**PCCustomizer** is a PC parts price-comparison assistant. It scrapes product data from CoolPC (原價屋), lets users build custom quote menus, and generates price estimates in one click.

| Item | Details |
|------|---------|
| Framework | .NET MAUI 10 + Blazor Hybrid |
| UI Library | MudBlazor 9 |
| Database | SQLite + EF Core 10 (`EnsureCreatedAsync`, no migrations) |
| Language | C# + Razor |
| Target Platforms | Windows (primary), Android, iOS, macOS Catalyst |

---

## 2. Directory Structure

```
PCCustomizer/
├── MauiProgram.cs              # DI container setup (all services registered here)
├── App.xaml / App.xaml.cs      # MAUI App entry point
├── MainPage.xaml               # BlazorWebView host
│
├── Components/
│   ├── BaseComponent.cs        # Base class for all Razor components (injects NotificationService)
│   ├── _Imports.razor          # Global @using directives
│   ├── Routes.razor            # Route definitions
│   ├── General/                # Shared small components
│   │   ├── ComputerInfoDialog.razor
│   │   └── PriceAlertText.razor
│   ├── Home/                   # Home page-specific components
│   │   ├── ProductContent.razor    # Product list (MudTabs)
│   │   └── CurrentMenuPopover.razor # Active menu popover
│   ├── Layout/                 # Layout components
│   │   ├── MainLayout.razor    # Root layout (AppBar + fixed sidebar)
│   │   ├── MyNavMenu.razor     # Sidebar navigation (MudNavMenu)
│   │   └── LoadingOverlay.razor # Global loading mask
│   └── Pages/
│       ├── Home.razor          # Home page (product browsing + menu operations)
│       ├── Menu.razor          # Menu management page
│       └── Setting.razor       # Settings page
│
├── Services/
│   ├── IDataService.cs / DataService.cs          # Data sync (Singleton)
│   ├── IMenuService.cs / MenuService.cs          # Menu CRUD (Scoped)
│   ├── ICategoryService.cs / CategoryService.cs  # Product category queries (Scoped)
│   ├── ICoolPcService.cs / CoolPcService.cs      # CoolPC HTTP client (Transient via HttpClientFactory)
│   ├── IHardwareService.cs / HardwareService.cs  # Hardware scanning (Singleton)
│   ├── IThemeService.cs / ThemeService.cs        # Theme switching (Singleton)
│   ├── INotificationService.cs / NotificationService.cs # Snackbar notifications (Scoped)
│   └── IUpdateCheckService.cs / UpdateCheckService.cs   # GitHub version check (Transient)
│
├── Models/
│   ├── Category.cs / Subcategory.cs / Product.cs  # Product data (loaded from network JSON)
│   ├── MenuCategory.cs / MenuProduct.cs           # User menu data
│   ├── DTOs/                                       # Data Transfer Objects
│   └── Hardware/                                   # Hardware info models
│
└── Data/
    └── AppDbContext.cs          # EF Core DbContext (with Fluent API configuration)
```

---

## 3. Service Lifetimes (Critical)

| Service | Lifetime | Notes |
|---------|---------|-------|
| `DataService` | **Singleton** | Owns `IsGlobalLoading`, `IsLoading` state shared across components |
| `ThemeService` | **Singleton** | Global theme state |
| `HardwareService` | **Singleton** | Caches hardware scan results |
| `MenuService` | **Scoped** | Depends on EF Core DbContext (Scoped) |
| `CategoryService` | **Scoped** | Depends on EF Core DbContext (Scoped) |
| `NotificationService` | **Scoped** | Depends on MudBlazor ISnackbar (Scoped) |
| `CoolPcService` | **Transient** | Managed via IHttpClientFactory |
| `UpdateCheckService` | **Transient** | Managed via IHttpClientFactory |

> **Rule**: `DataService` is a Singleton. When it needs Scoped services (e.g. `INotificationService`, `AppDbContext`), always use `serviceProvider.CreateScope()` to create a manual scope. Never hold a direct reference to a Scoped service inside a Singleton.

---

## 4. Database Rules

### Schema Change Protocol

**After any Model change (add/remove/rename a field), you MUST warn the user:**

> "This change modifies the database schema. Please delete the local PCCustomizer.db3 file and restart the app.
> Silent query failures will occur otherwise — EF Core uses `EnsureCreatedAsync`, which does not support `ALTER TABLE`."

DB file path (Windows):
```
C:\Users\{username}\AppData\Local\Packages\com.companyname.pccustomizer_cgsmvjbq0fw2p\LocalState\PCCustomizer.db3
```

### Entity Relationships

- `Category` 1 → N `Subcategory` (Cascade Delete)
- `Subcategory` 1 → N `Product` (linked via `SubcategoryName` string)
- `MenuCategory` 1 → N `MenuProduct` (Cascade Delete)
- `MenuProduct` stores snapshot strings (`CategoryName`, `ProductName`, etc.) — not foreign keys to products

---

## 5. Global Loading Overlay

```
App start → MainLayout.OnInitializedAsync → DataService.SeedDataIfNeededAsync()
               ↓
           SeedDataIfNeededAsync starts → IsLoading = true; IsGlobalLoading = true (overlay shown)
               ↓
           SeedDataIfNeededAsync ends → IsLoading = false (IsGlobalLoading still true)
               ↓
           Home.razor HandleDataStateChanged fires → DB read complete → SetGlobalLoading(false) (overlay hidden)
```

- `DataService.IsGlobalLoading` (default `true`): controls `LoadingOverlay` component visibility
- `DataService.IsLoading`: used only to disable UI buttons (prevents duplicate SeedData triggers)
- `DataService.LoadingMessage`: text displayed on the overlay; set together via `SetGlobalLoading`
- `DataService.SetGlobalLoading(bool value, string message)`: **the only correct way to control the overlay**
- `LoadingOverlay` is a standalone component that subscribes directly to `DataService.OnStateChanged` — it does **not** trigger a full Layout re-render

### Overlay Trigger Scenarios

| Scenario | Open Overlay | Message | Close Overlay |
|----------|-------------|---------|---------------|
| App startup / manual data refresh | Inside `SeedDataIfNeededAsync` (sets `IsGlobalLoading = true` directly) | Default: "更新原價屋資訊中..." | `Home.razor` `UpdateData()` or `HandleDataStateChanged()` calls `SetGlobalLoading(false)` |
| Switch main category | `Home.razor` `OnSelectedCategoryChanged` calls `SetGlobalLoading(true, "商品資訊載入中...")` | "商品資訊載入中..." | Same method calls `SetGlobalLoading(false)` at the end |

> **Note**: The `finally` block in `SeedDataIfNeededAsync` only closes `IsLoading`, **not `IsGlobalLoading`**.
> Responsibility for closing the global overlay belongs to `Home.razor`, not `DataService`.

---

## 6. Razor Component Rules

1. **All page components must inherit `BaseComponent`** — automatically provides `NotificationService` and `OpenExternalLink`
2. **`MainLayout` must NOT inherit `BaseComponent`** — it extends `LayoutComponentBase`, which is incompatible
3. When using MudBlazor components, use PascalCase parameters: `Class` not `class`, `Style` not `style`
4. For async operations on `MudSelect`, use `Value` + `ValueChanged` — do NOT use `@bind-Value`
5. `DrawerVariant.Permanent` does **not exist** in MudBlazor 9 — use `DrawerVariant.Persistent`
6. **Sidebar drawer uses Mini Variant**: `DrawerVariant.Mini` + `OpenMiniOnHover="true"`, `_drawerOpen` defaults to `false` (collapsed on start, auto-expands on hover, auto-collapses on mouse-out). **Do not revert to Persistent or Temporary.**

---

## 7. Known Technical Debt

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| M-08 | `AddMenuProduct` makes 5–6 separate DB queries | `MenuService.cs:89` | Triggered per user interaction; limited performance impact |
| P-01 | `HardwareService` cache is non-atomic | `HardwareService.cs` | Concurrent calls may trigger multiple scans; consider `SemaphoreSlim` |
| P-02 | `GetCategoriesWithDetailsAsync` loads all products | `CategoryService.cs` | Full load every call; potential bottleneck with large datasets |
| S-01 | GitHub API anonymous rate limit | `UpdateCheckService.cs` | 60 req/hr; consider caching last-checked timestamp |

---

## 8. Prohibited Actions

- **Do NOT inject Scoped services directly into Singleton services** (causes Captive Dependency)
- **Do NOT use `DrawerVariant.Permanent`** (not supported in MudBlazor 9)
- **Do NOT modify Model fields without warning the user to delete the DB file**
- **Do NOT re-inject `INotificationService` in `BaseComponent` subclasses** (already injected by base)
- **Do NOT inherit `BaseComponent` in `MainLayout`** (incompatible with `LayoutComponentBase`)
- **`NavMenu.razor` has been removed** — use `MyNavMenu.razor` for all navigation
- **Do NOT set `IsGlobalLoading` directly** (property has `private set`) — always use `DataService.SetGlobalLoading(bool, string)`
- **Do NOT close the global overlay inside `DataService.SeedDataIfNeededAsync`'s `finally` block** — overlay close responsibility belongs to `Home.razor`

---

## 9. Common Agent Workflows

### Add a New Service

1. Create `Services/IXxxService.cs` interface
2. Create `Services/XxxService.cs` implementation
3. Register in `MauiProgram.cs` with correct lifetime
4. Verify lifetime is compatible with all dependencies (avoid Captive Dependency)

### Add a New Page

1. Create `Components/Pages/Xxx.razor`
2. Add `@page "/xxx"` route directive
3. Add `@inherits BaseComponent`
4. Add a corresponding `MudNavLink` in `MyNavMenu.razor`

### Modify an EF Core Model

1. Update the relevant model in `Models/`
2. **Warn the user to delete the local PCCustomizer.db3**
3. Update `OnModelCreating` Fluent API config in `AppDbContext.cs` if needed
