using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using YALCINDORSE.Components.Pages;

namespace YALCINDORSE.Windows
{
    public class GenericBlazorWindow : ContentPage
    {
        public GenericBlazorWindow(string title, Type componentType, Dictionary<string, object>? parameters = null)
        {
            Title = title;
            BackgroundColor = Colors.Transparent;

            var blazorWebView = new BlazorWebView
            {
                HostPage = "wwwroot/index.html"
            };

            var rootParams = new Dictionary<string, object?>
            {
                { "ComponentType", componentType },
                { "ComponentParameters", parameters ?? new Dictionary<string, object>() },
                { "WindowTitle", title }
            };

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(GenericWindowRoot),
                Parameters = rootParams
            });

            Content = blazorWebView;
        }
    }
}
