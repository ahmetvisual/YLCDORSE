namespace YALCINDORSE
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

#if WINDOWS
            // Login ekranının etrafındaki boşlukları "kırpmak" için uygulamanın ilk açılış boyutunu küçültüyoruz
            window.Width = 850;
            window.Height = 560;

            // Ekranın tam ortasında açılması için:
            window.Created += (s, e) =>
            {
                var uiWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (uiWindow != null)
                {
                    var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(uiWindow);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                    
                    var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                    if (displayArea != null && appWindow != null)
                    {
                        var centeredPosition = new global::Windows.Graphics.PointInt32(
                            (displayArea.WorkArea.Width - appWindow.Size.Width) / 2,
                            (displayArea.WorkArea.Height - appWindow.Size.Height) / 2
                        );
                        appWindow.Move(centeredPosition);

                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                        {
                            presenter.SetBorderAndTitleBar(false, false);
                            presenter.IsResizable = false;
                            presenter.IsMaximizable = false;
                        }
                    }
                }
            };
#endif

            return window;
        }
    }
}
