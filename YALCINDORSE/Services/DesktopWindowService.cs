using Microsoft.Maui.ApplicationModel;
using YALCINDORSE.Windows;
using System.Collections.Concurrent;

namespace YALCINDORSE.Services
{
    public class DesktopWindowService
    {
        private readonly ConcurrentDictionary<Type, Window> _openWindows = new();

        // Close-guard: bir Blazor component pencerenin kapanmasini engelleyebilir.
        // ShouldBlock = true donerse, pencere kapanmaz; OnBlocked geri cagrilarak
        // component'in (ornegin "kaydedilmemis degisiklikler" dialogu gostermesi) tetiklenir.
        private readonly ConcurrentDictionary<Type, (Func<bool> ShouldBlock, Func<Task>? OnBlocked)> _closeGuards = new();

        public void RegisterCloseGuard<TComponent>(Func<bool> shouldBlock, Func<Task>? onBlocked = null)
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
            _closeGuards[typeof(TComponent)] = (shouldBlock, onBlocked);
        }

        public void UnregisterCloseGuard<TComponent>()
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
            _closeGuards.TryRemove(typeof(TComponent), out _);
        }

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
                    _closeGuards.TryRemove(componentType, out _);
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

                        // Close-guard: kayit edilmemis degisikliklerde component dialog gosterebilsin
                        appWindow.Closing += (sender, args) =>
                        {
                            try
                            {
                                if (_closeGuards.TryGetValue(componentType, out var guard) && guard.ShouldBlock())
                                {
                                    args.Cancel = true;
                                    if (guard.OnBlocked != null) _ = guard.OnBlocked();
                                }
                            }
                            catch { /* guard hatasi pencereyi kilitlemesin */ }
                        };

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

                        // Pencereyi ANA PENCERENIN oldugu monitorde ortala.
                        // Ana pencerenin HWND'ini al -> hangi monitorde oldugunu bul (Nearest) ->
                        // o monitörün WorkArea'sini baz alarak yeni pencereyi merkezle.
                        // WorkArea.X / Y kullanmak zorunlu: ikincil monitorde WorkArea (1920,0)'dan
                        // basliyor, 0'dan degil. Yoksa pencere 1. monitore dusuyor.
                        var mainUiWin = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView
                                        as Microsoft.UI.Xaml.Window;
                        Microsoft.UI.Windowing.DisplayArea? displayArea = null;
                        if (mainUiWin != null)
                        {
                            var mainHandle   = WinRT.Interop.WindowNative.GetWindowHandle(mainUiWin);
                            var mainWinId    = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(mainHandle);
                            displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                                mainWinId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                        }
                        displayArea ??= Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                            windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

                        if (displayArea != null)
                        {
                            var wa = displayArea.WorkArea;
                            appWindow.Move(new global::Windows.Graphics.PointInt32(
                                wa.X + (wa.Width  - appWindow.Size.Width)  / 2,
                                wa.Y + (wa.Height - appWindow.Size.Height) / 2
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
            return OpenWindow<YALCINDORSE.Components.Pages.CRM.CariKartlari>("Cari Kartlar", 1080, 820);
        }

        /// <summary>
        /// Belirtilen component tipindeki pencereyi kapatir.
        /// Kaydet/iptal sonrasi formu otomatik kapatmak icin kullanilir.
        /// </summary>
        public void CloseWindow<TComponent>()
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
#if WINDOWS || MACCATALYST
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var componentType = typeof(TComponent);
                if (_openWindows.TryGetValue(componentType, out var window))
                {
                    Application.Current?.CloseWindow(window);
                    _openWindows.TryRemove(componentType, out _);
                }
            });
#endif
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
