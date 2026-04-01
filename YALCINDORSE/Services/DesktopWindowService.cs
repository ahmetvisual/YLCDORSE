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
                    return;

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
                    SetupCustomTitleBar(newWindow);
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
        private void SetupCustomTitleBar(Window mauiWindow)
        {
            var uiWindow = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (uiWindow == null) return;

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(uiWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // --- Native title bar'i Blazor icerigine genislet ---
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Sistem caption butonlarini tamamen gizle (kendi butonlarimizi kullaniyoruz)
            var transparent = Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0);
            titleBar.ButtonBackgroundColor = transparent;
            titleBar.ButtonInactiveBackgroundColor = transparent;
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(30, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(50, 255, 255, 255);
            titleBar.ButtonForegroundColor = transparent;
            titleBar.ButtonHoverForegroundColor = transparent;
            titleBar.ButtonPressedForegroundColor = transparent;
            titleBar.ButtonInactiveForegroundColor = transparent;

            // Yukseklik: Blazor titlebar 34px, bunu native olarak ayarla
            titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;

            // Drag alani ayarla
            UpdateDragRects(appWindow, uiWindow);

            // Pencere boyutu degisince drag alanini guncelle
            appWindow.Changed += (sender, args) =>
            {
                if (args.DidSizeChange && sender is Microsoft.UI.Windowing.AppWindow aw)
                {
                    UpdateDragRects(aw, uiWindow);
                }
            };

            // Pencereyi ortala
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                appWindow.Move(new global::Windows.Graphics.PointInt32(
                    (displayArea.WorkArea.Width - appWindow.Size.Width) / 2,
                    (displayArea.WorkArea.Height - appWindow.Size.Height) / 2
                ));
            }

            // Owner ayarla (alt pencere ana pencerenin arkasina dusmesin)
            var mainWindow = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (mainWindow != null && mainWindow != uiWindow)
            {
                var mainHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                SetOwner(windowHandle, mainHandle);
            }
        }

        private void UpdateDragRects(Microsoft.UI.Windowing.AppWindow appWindow, Microsoft.UI.Xaml.Window uiWindow)
        {
            try
            {
                var scale = uiWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
                var titleBarH = (int)(34 * scale);
                var btnAreaW = (int)(120 * scale); // 3 buton alani
                var winW = appWindow.Size.Width;

                // Buton alani passthrough (tiklanabilir), geri kalan caption (suruklenebilir)
                appWindow.TitleBar.SetDragRectangles(new[] {
                    new global::Windows.Graphics.RectInt32(0, 0, Math.Max(winW - btnAreaW, 0), titleBarH)
                });
            }
            catch { }
        }

        private const int GWLP_HWNDPARENT = -8;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static void SetOwner(IntPtr childHandle, IntPtr ownerHandle)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr(childHandle, GWLP_HWNDPARENT, ownerHandle);
            else
                SetWindowLong(childHandle, GWLP_HWNDPARENT, ownerHandle.ToInt32());
        }
#endif
    }
}
