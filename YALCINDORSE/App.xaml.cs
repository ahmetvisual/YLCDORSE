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
            // Not: CSS'teki 768px breakpoint'ini aşmak için genişliği 850 yapıyoruz.
            window.Width = 850;
            window.Height = 560;
#endif

            return window;
        }
    }
}
