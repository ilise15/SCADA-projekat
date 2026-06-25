using System.Windows;
using ScadaGUI.ViewModels;

namespace ScadaGUI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataConcentrator.AuthService.Instance.AdminTimedOut += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var login = new LoginWindow();
                    login.Show();
                    this.Close();
                });
            };
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(DataContext as MainViewModel);
            win.Owner = this;
            win.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataConcentrator.PLC.instance?.Abort();
        }
    }
}
