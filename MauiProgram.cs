using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using PCCustomizer.Services;
using PCCustomizer.Data;
using Microsoft.EntityFrameworkCore; 

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
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<HttpClient>();
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "PCCustomizer.db3");
            // 如果table有更新，記得去這個路徑把PCCustomizer.db3刪掉，系統打開會自己重建
            // C:\Users\Gusty\AppData\Local\Packages\com.companyname.pccustomizer_9zz4h110yvjzm\LocalState\PCCustomizer.db3
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            builder.Services.AddTransient<IDataService, DataService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
