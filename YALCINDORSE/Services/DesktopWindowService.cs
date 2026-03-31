using Microsoft.Maui.ApplicationModel;
using YALCINDORSE.Windows;

namespace YALCINDORSE.Services
{
    public class DesktopWindowService
    {
        private Window? _customerCardsWindow;

        public bool OpenCustomerCardsWindow()
        {
#if WINDOWS || MACCATALYST
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_customerCardsWindow != null)
                    return;

                _customerCardsWindow = new Window(new CustomerCardsWindowPage())
                {
                    Title = "Cari Kartlar",
                    Width = 1440,
                    Height = 920
                };

                _customerCardsWindow.Destroying += HandleCustomerCardsWindowDestroying;
                Application.Current?.OpenWindow(_customerCardsWindow);
            });

            return true;
#else
            return false;
#endif
        }

        private void HandleCustomerCardsWindowDestroying(object? sender, EventArgs e)
        {
            if (_customerCardsWindow != null)
                _customerCardsWindow.Destroying -= HandleCustomerCardsWindowDestroying;

            _customerCardsWindow = null;
        }
    }
}
