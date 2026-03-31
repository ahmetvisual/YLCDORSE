using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using YALCINDORSE.Components.Pages.CRM;

namespace YALCINDORSE.Windows
{
    public class CustomerCardsWindowPage : ContentPage
    {
        public CustomerCardsWindowPage()
        {
            Title = "Cari Kartlar";
            BackgroundColor = Colors.Transparent;

            var blazorWebView = new BlazorWebView
            {
                HostPage = "wwwroot/index.html"
            };

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(CariKartlariWindowRoot)
            });

            Content = blazorWebView;
        }
    }
}
