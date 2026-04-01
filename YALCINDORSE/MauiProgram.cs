using Microsoft.Extensions.Logging;
using YALCINDORSE.Helpers;
using YALCINDORSE.Services;

namespace YALCINDORSE
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

            builder.Services.AddMauiBlazorWebView();

            // Database & Services
            builder.Services.AddSingleton<DatabaseHelper>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<DesktopWindowService>();
            builder.Services.AddTransient<UserService>();
            builder.Services.AddTransient<CustomerService>();
            builder.Services.AddTransient<QuoteService>();
            builder.Services.AddTransient<TouchService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
