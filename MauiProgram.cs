using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using PCCustomizer.Data;
using PCCustomizer.Services;
using System.Text;

namespace PCCustomizer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // 服務註冊區
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();
            builder.Services.AddSingleton(Preferences.Default);
            builder.Services.AddHttpClient<IUpdateCheckService, UpdateCheckService>(client =>
            {
                // 這是 GitHub API 要求的標頭
                client.DefaultRequestHeaders.Add("User-Agent", "PCCustomizer-Update-Check");
            });
            builder.Services.AddHttpClient("DataService", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PCCustomizer");
            });
            builder.Services.AddSingleton<IDataService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("DataService");
                return new DataService(httpClient, sp);
            });
            builder.Services.AddHttpClient<ICoolPcService, CoolPcService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");
            })
            // 停用自動 Cookie 管理，讓程式碼手動傳遞 Cookie 標頭（與原設計一致）
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<IHardwareService, HardwareService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IMenuService, MenuService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "PCCustomizer.db3");
            // 如果 table 結構有更新，請刪除 AppDataDirectory 下的 PCCustomizer.db3，系統啟動時會自動重建
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
