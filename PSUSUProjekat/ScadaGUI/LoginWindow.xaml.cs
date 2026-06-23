using System.Windows;
using System.Windows.Controls;
using DataConcentrator;

namespace ScadaGUI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            RoleBox.SelectedIndex = 0;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string role = (RoleBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ShowError("Popunite sva polja.");
                return;
            }

            if (AuthService.Instance.Login(username, password, role))
            {
                var main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                ShowError("Neispravni podaci. Pokušajte ponovo.");
            }
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
