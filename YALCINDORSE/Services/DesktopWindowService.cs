using Microsoft.Maui.ApplicationModel;
using YALCINDORSE.Windows;
using System.Collections.Concurrent;

namespace YALCINDORSE.Services
{
    public class DesktopWindowService
    {
        private readonly ConcurrentDictionary<Type, Window> _openWindows = new();

        public bool OpenWindow<TComponent>(string title, int width = 1200, int height = 800, Dictionary<string, object>? parameters = null)
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
#if WINDOWS || MACCATALYST
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var componentType = typeof(TComponent);

                if (_openWindows.TryGetValue(componentType, out var existingWindow))
                {
                    // Farkli parametrelerle aciliyorsa (ornegin farkli QuoteId), eskiyi kapat
                    if (parameters != null && parameters.Count > 0)
                    {
                        Application.Current?.CloseWindow(existingWindow);
                        _openWindows.TryRemove(componentType, out _);
                    }
                    else
                    {
                        // Pencere zaten acik: minimize edilmisse restore et, one getir
#if WINDOWS
                        ActivateExistingWindow(existingWindow);
#endif
                        return;
                    }
                }

                var page = new GenericBlazorWindow(title, componentType, parameters);
                var newWindow = new Window(page)
                {
                    Title = title,
                    Width = width,
                    Height = height
                };

                newWindow.Destroying += (s, e) =>
                {
                    _openWindows.TryRemove(componentType, out _);
                };

                newWindow.Created += (s, e) =>
                {
#if WINDOWS
                    var uiWindow = newWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (uiWindow != null)
                    {
                        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(uiWindow);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                        // Title bar rengini degistir (koyu mavi)
                        appWindow.TitleBar.BackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 15, 37, 72);
                        appWindow.TitleBar.ForegroundColor = Microsoft.UI.Colors.White;
                        appWindow.TitleBar.InactiveBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 20, 45, 85);
                        appWindow.TitleBar.InactiveForegroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180);
                        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 15, 37, 72);
                        appWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        appWindow.TitleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 30, 60, 110);
                        appWindow.TitleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                        appWindow.TitleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 10, 25, 55);
                        appWindow.TitleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 20, 45, 85);
                        appWindow.TitleBar.ButtonInactiveForegroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180);

                        // Pencereyi ortala
                        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                        if (displayArea != null)
                        {
                            appWindow.Move(new global::Windows.Graphics.PointInt32(
                                (displayArea.WorkArea.Width - appWindow.Size.Width) / 2,
                                (displayArea.WorkArea.Height - appWindow.Size.Height) / 2
                            ));
                        }

                        // Gorev cubugunda bagımsız buton goster:
                        // SetOwner KALDIRILDI - owner iliskisi goreve cubugunu engelliyor.
                        // WS_EX_APPWINDOW: Windows'a "bu pencere gorev cubugunda ayri gorunsun" der.
                        // WS_EX_TOOLWINDOW: varsa kaldir (gorev cubugundan gizler).
                        var exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                        SetWindowLong(windowHandle, GWL_EXSTYLE,
                            (exStyle | WS_EX_APPWINDOW) & ~WS_EX_TOOLWINDOW);
                    }
#endif
                };

                _openWindows.TryAdd(componentType, newWindow);
                Application.Current?.OpenWindow(newWindow);
            });

            return true;
#else
            return false;
#endif
        }

        public bool OpenCustomerCardsWindow()
        {
            return OpenWindow<YALCINDORSE.Components.Pages.CRM.CariKartlari>("Cari Kartlar", 1440, 920);
        }

        public void MaximizeMainWindow()
        {
#if WINDOWS
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window != null)
                {
                    var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.SetBorderAndTitleBar(true, true);
                        presenter.IsResizable = true;
                        presenter.IsMaximizable = true;
                        presenter.Maximize();
                    }
                }
            });
#endif
        }

#if WINDOWS
        /// <summary>
        /// Mevcut pencereyi minimize edilmisse restore eder ve one getirir.
        /// Kullanici menuden tekrar tiklayinca formu gorev cubugundan geri getirmek icin kullanilir.
        /// </summary>
        private static void ActivateExistingWindow(Window window)
        {
            var uiWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (uiWindow == null) return;

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(uiWindow);
            // Minimize edilmisse normal hale getir, diger durumda oldugu gibi birak
            ShowWindow(hWnd, SW_RESTORE);
            // Pencereyi one getir ve aktif yap
            SetForegroundWindow(hWnd);
        }

        private const int GWL_EXSTYLE      = -20;
        private const int WS_EX_APPWINDOW  = 0x00040000;  // Gorev cubugunda goster
        private const int WS_EX_TOOLWINDOW = 0x00000080;  // Gorev cubugunden gizle (istemiyoruz)
        private const int SW_RESTORE        = 9;           // Minimize edilmis pencereyi geri ac

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
#endif
    }
}
