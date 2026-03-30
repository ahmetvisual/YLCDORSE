namespace YALCINDORSE.Services
{
    public class ThemeService
    {
        private const string ThemeKey = "app_theme";
        public string CurrentTheme { get; private set; } = "light";
        public bool IsDark => CurrentTheme == "dark";

        public event Action? OnThemeChanged;

        public ThemeService()
        {
            LoadTheme();
        }

        private void LoadTheme()
        {
            try
            {
                CurrentTheme = Preferences.Get(ThemeKey, "light");
            }
            catch
            {
                CurrentTheme = "light";
            }
        }

        public void SetTheme(string theme)
        {
            CurrentTheme = theme;
            try { Preferences.Set(ThemeKey, theme); } catch { }
            OnThemeChanged?.Invoke();
        }

        public void Toggle()
        {
            SetTheme(IsDark ? "light" : "dark");
        }
    }
}
