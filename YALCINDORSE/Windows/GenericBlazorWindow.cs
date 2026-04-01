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

#if WINDOWS
            blazorWebView.BlazorWebViewInitialized += (sender, args) =>
            {
                var webView2 = args.WebView;
                webView2.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    var message = e.TryGetWebMessageAsString();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        switch (message)
                        {
                            case "MINIMIZE":
                                MinimizeWindow();
                                break;
                            case "MAXIMIZE_TOGGLE":
                                ToggleMaximize();
                                break;
                            case "CLOSE":
                                CloseWindow();
                                break;
                        }
                    });
                };
            };
#endif

            Content = blazorWebView;
        }

#if WINDOWS
        private void MinimizeWindow()
        {
            try
            {
                var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    ShowWindow(hwnd, 6); // SW_MINIMIZE
                }
            }
            catch { }
        }

        private void ToggleMaximize()
        {
            try
            {
                var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var wId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wId);
                    if (appWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                    {
                        if (p.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
                            p.Restore();
                        else
                            p.Maximize();
                    }
                }
            }
            catch { }
        }

        private void CloseWindow()
        {
            try
            {
                var mauiWindow = this.Window;
                if (mauiWindow != null)
                    Application.Current?.CloseWindow(mauiWindow);
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);
#endif
    }
}
