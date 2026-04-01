using Microsoft.Maui.ApplicationModel;
using YALCINDORSE.Windows;
using System.Collections.Concurrent;

namespace YALCINDORSE.Services
{
    public class DesktopWindowService
    {
        // Türüne (Type) göre açık pencereleri takip eden sözlük
        private readonly ConcurrentDictionary<Type, Window> _openWindows = new();

        /// <summary>
        /// Sadece tek bir kopyası açık kalacak şekilde bir Blazor sayfasını MDI penceresi olarak açar.
        /// </summary>
        public bool OpenWindow<TComponent>(string title, int width = 1200, int height = 800, Dictionary<string, object>? parameters = null) 
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
#if WINDOWS || MACCATALYST
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var componentType = typeof(TComponent);

                // Eğer bu tipte bir pencere zaten açıksa, odaklan ve yeni açma
                if (_openWindows.TryGetValue(componentType, out var existingWindow))
                {
                    // MAUI tarafında var olan pencereyi öne çıkarma mantığı (şimdilik sadece geri dönüyor)
                    return;
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
                        
                        // Native title bar'i tamamen kaldır, border'i koru
                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter owPresenter)
                        {
                            owPresenter.SetBorderAndTitleBar(true, false);
                        }

                        // Ust 34px'i native drag alani olarak tanimla (InputNonClientPointerSource)
                        // Bu API WebView2'den ONCE calisir, sifir gecikme ile surukleme saglar
                        try
                        {
                            var nonClientSrc = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(windowId);
                            var scale = uiWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
                            var dragH = (int)(34 * scale);
                            var btnW = (int)(112 * scale);
                            var winW = appWindow.Size.Width;

                            nonClientSrc.SetRegionRects(
                                Microsoft.UI.Input.NonClientRegionKind.Caption,
                                new[] { new global::Windows.Graphics.RectInt32(0, 0, winW - btnW, dragH) }
                            );

                            // Pencere boyutu degisince drag alani guncelle
                            appWindow.Changed += (sender, args) =>
                            {
                                if (args.DidSizeChange && sender is Microsoft.UI.Windowing.AppWindow aw)
                                {
                                    try
                                    {
                                        nonClientSrc.SetRegionRects(
                                            Microsoft.UI.Input.NonClientRegionKind.Caption,
                                            new[] { new global::Windows.Graphics.RectInt32(0, 0, aw.Size.Width - btnW, dragH) }
                                        );
                                    }
                                    catch { }
                                }
                            };
                        }
                        catch { }

                        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                        if (displayArea != null && appWindow != null)
                        {
                            var centeredPosition = new global::Windows.Graphics.PointInt32(
                                (displayArea.WorkArea.Width - appWindow.Size.Width) / 2,
                                (displayArea.WorkArea.Height - appWindow.Size.Height) / 2
                            );
                            appWindow.Move(centeredPosition);
                        }

                        // Alt pencerenin hiçbir zaman Ana pencerenin (Anasayfanın) arkasına düşmemesi için (Owner yapılması)
                        var mainWindow = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                        if (mainWindow != null && mainWindow != uiWindow)
                        {
                            var mainHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                            SetOwner(windowHandle, mainHandle);
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

        // Eski uyumluluk için (Cari kartları MDI'si de buraya bağlıyoruz)
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
                        // Başarılı girişten sonra ana uygulamanın tüm pencerelerini/başlığını açıyoruz (Login çerçevesizdi)
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
