using Microsoft.Extensions.Logging;
using YALCINDORSE.Helpers;
using YALCINDORSE.Services;

namespace YALCINDORSE
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
#if WINDOWS
            // Windows Server 2019 + RDP: GPU olmayan ortamlarda yazilim tabanli render
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
                "--disable-gpu --disable-gpu-compositing --use-angle=warp --disable-gpu-rasterization");
#endif
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Database & Services
            builder.Services.AddSingleton<DatabaseHelper>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<DesktopWindowService>();
            builder.Services.AddTransient<UserService>();
            builder.Services.AddTransient<CustomerService>();
            builder.Services.AddTransient<QuoteService>();
            builder.Services.AddTransient<QuoteSpecService>();
            builder.Services.AddTransient<QuoteAttachmentService>();
            builder.Services.AddTransient<QuoteDocumentService>();
            builder.Services.AddTransient<TouchService>();
            builder.Services.AddTransient<ArabaslikService>();
            builder.Services.AddTransient<QuoteLookupService>();
            builder.Services.AddTransient<ZirveService>();
            builder.Services.AddTransient<FirmaService>();
            builder.Services.AddTransient<LastikService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
