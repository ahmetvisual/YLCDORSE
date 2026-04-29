using Microsoft.Maui.ApplicationModel;
using YALCINDORSE.Windows;
using System.Collections.Concurrent;

namespace YALCINDORSE.Services
{
    public class DesktopWindowService
    {
        private readonly ConcurrentDictionary<Type, Window> _openWindows = new();

        // Ana pencerenin MAUI Window referansi — MaximizeMainWindow'da set edilir.
        // OpenWindow'da her acilista taze HWND alinir, boylece kullanici ana pencereyi
        // baska monitore tasidiktan sonra da dogru monitoru bulabiliriz.
        private Window? _mainMauiWindow = null;
        private IntPtr _mainWindowHandle = IntPtr.Zero;


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

                        // Gorev cubugunda bagımsız buton goster
                        var exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                        SetWindowLong(windowHandle, GWL_EXSTYLE,
                            (exStyle | WS_EX_APPWINDOW) & ~WS_EX_TOOLWINDOW);

                        // Ana pencerenin HWND'ini her seferinde taze al.
                        // MAUI Window referansindan guncel handle'i cekiyoruz — kullanici
                        // ana pencereyi baska monitore tasimis olsa bile MonitorFromWindow
                        // dogru monitoru doner.
                        // Not: Created event icerisinde olduğumuz icin yeni pencere henuz
                        // Application.Windows'a eklenmemis olabilir; FirstOrDefault(w != newWindow)
                        // yerine dogrudan _mainMauiWindow referansini kullaniyoruz.
                        if (_mainMauiWindow != null)
                        {
                            var mainUiRef = _mainMauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                            if (mainUiRef != null)
                            {
                                var freshHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainUiRef);
                                if (freshHandle != IntPtr.Zero)
                                    _mainWindowHandle = freshHandle;
                            }
                        }

                        // Monitor bilgisini ana pencere uzerinden al.
                        // Win32 MonitorFromWindow her zaman guncel pencere pozisyonunu kullanir;
                        // WinAppSDK DisplayArea API'sinden daha guvenilir (monitor degistirme sonrasi).
                        int monX = 0, monY = 0, monW = 0, monH = 0;
                        bool monFound = false;
                        if (_mainWindowHandle != IntPtr.Zero)
                        {
                            var hMon = MonitorFromWindow(_mainWindowHandle, MONITOR_DEFAULTTONEAREST);
                            if (hMon != IntPtr.Zero)
                            {
                                var mi = new MONITORINFO();
                                mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>();
                                if (GetMonitorInfo(hMon, ref mi))
                                {
                                    monX = mi.rcWork.Left;
                                    monY = mi.rcWork.Top;
                                    monW = mi.rcWork.Right  - mi.rcWork.Left;
                                    monH = mi.rcWork.Bottom - mi.rcWork.Top;
                                    monFound = true;
                                }
                            }
                        }
                        if (!monFound)
                        {
                            var fallback = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                                windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                            if (fallback != null)
                            {
                                monX = fallback.WorkArea.X;
                                monY = fallback.WorkArea.Y;
                                monW = fallback.WorkArea.Width;
                                monH = fallback.WorkArea.Height;
                                monFound = true;
                            }
                        }

                        if (monFound)
                        {
                            // MoveAndResize'i Low-priority ile kuyruğa al.
                            // MAUI kendi Width/Height'ini primary monitörün DPI'sina göre piksel'e
                            // cevirir ve pencereyi oraya koyar. Biz Low-priority dispatch ile
                            // MAUI bittikten sonra devreye girip HEM pozisyonu HEM boyutu
                            // hedef monitörün DPI'sina göre atomik olarak set ediyoruz.
                            // Böylece farklı DPI'lı monitörlerde de boyut doğru çıkar.
                            int capturedMonX = monX, capturedMonY = monY;
                            int capturedMonW = monW, capturedMonH = monH;
                            var capturedMainHandle = _mainWindowHandle;
                            uiWindow.DispatcherQueue.TryEnqueue(
                                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                                () =>
                                {
                                    // Hedef monitörün DPI'sini al (farklı ölçekleme desteği)
                                    uint dpiX = 96, dpiY = 96;
                                    if (capturedMainHandle != IntPtr.Zero)
                                    {
                                        var hMonDpi = MonitorFromWindow(capturedMainHandle, MONITOR_DEFAULTTONEAREST);
                                        if (hMonDpi != IntPtr.Zero)
                                            GetDpiForMonitor(hMonDpi, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                                    }

                                    // width/height DIP (device-independent pixel) cinsinden gelir.
                                    // Hedef monitörün DPI'sına göre fiziksel piksele çevir.
                                    int physW = (int)Math.Round(width  * dpiX / 96.0);
                                    int physH = (int)Math.Round(height * dpiY / 96.0);

                                    int posX = capturedMonX + (capturedMonW - physW) / 2;
                                    int posY = capturedMonY + (capturedMonH - physH) / 2;

                                    // Pozisyon + boyutu atomik set et — SetWindowPos(NOSIZE) yerine
                                    // bu kullanılıyor çünkü MAUI'nin DPI dönüşümü yanlış monitörü baz alır.
                                    appWindow.MoveAndResize(new global::Windows.Graphics.RectInt32(posX, posY, physW, physH));
                                });
                        }
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
                var mauiWin = Application.Current?.Windows.FirstOrDefault();
                var window = mauiWin?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window != null)
                {
                    // MAUI Window referansini sakla — OpenWindow her acilista taze HWND alir.
                    _mainMauiWindow = mauiWin;
                    var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    _mainWindowHandle = windowHandle;
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

        private const int  GWL_EXSTYLE             = -20;
        private const int  WS_EX_APPWINDOW         = 0x00040000;
        private const int  WS_EX_TOOLWINDOW        = 0x00000080;
        private const int  SW_RESTORE              = 9;
        private const int  MONITOR_DEFAULTTONEAREST = 2;
        private const uint MDT_EFFECTIVE_DPI       = 0;   // GetDpiForMonitor: ekranın efektif DPI'si

        // SetWindowPos flagleri (artık yalnızca ActivateExistingWindow'da kullanılıyor)
        private const uint SWP_NOSIZE       = 0x0001;
        private const uint SWP_NOZORDER     = 0x0004;
        private const uint SWP_NOACTIVATE   = 0x0010;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, uint dpiType,
            out uint dpiX, out uint dpiY);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int  cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int  dwFlags;
        }
#endif
    }
}
