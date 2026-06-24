using System.Windows;
using DataConcentrator;

namespace ScadaGUI
{
    public partial class App :Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ApplyTheme(false);

            AuthService.Instance.AdminTimedOut += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Admin sesija istekla zbog neaktivnosti. Prijavite se ponovo.",
                        "Sesija istekla", MessageBoxButton.OK, MessageBoxImage.Warning);
                    var login = new Views.LoginWindow();
                    login.Show();
                    foreach(Window w in Current.Windows)
                        if(!(w is Views.LoginWindow)) w.Close();
                });
            };
        }

        public static void ApplyTheme(bool dark)
        {
            var dict = new ResourceDictionary();
            if(dark)
                dict.Source = new System.Uri("pack://application:,,,/Resources/DarkTheme.xaml",
                    System.UriKind.Absolute);
            else
                dict.Source = new System.Uri("pack://application:,,,/Resources/LightTheme.xaml",
                    System.UriKind.Absolute);
            Current.Resources.MergedDictionaries.Clear();
            Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
