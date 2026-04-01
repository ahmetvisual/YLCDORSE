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
                    if (message == "DRAG_START")
                    {
                        MainThread.BeginInvokeOnMainThread(() => DragWindow());
                    }
                    else if (message == "MAXIMIZE_TOGGLE")
                    {
                        MainThread.BeginInvokeOnMainThread(() => ToggleMaximize());
                    }
                };
            };
#endif

            Content = blazorWebView;
        }

#if WINDOWS
        private void DragWindow()
        {
            try
            {
                var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    ReleaseCapture();
                    SendMessage(hwnd, 0x00A1, (nint)0x0002, 0); // WM_NCLBUTTONDOWN, HTCAPTION
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
#endif
    }
}
